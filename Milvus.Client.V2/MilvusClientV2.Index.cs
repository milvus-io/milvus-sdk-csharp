using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates one or more indexes on a collection, mirroring the Java SDK's <c>createIndex()</c>: each
    /// <see cref="CreateIndexReq.Indexes" /> entry issues its own create-index RPC and, when
    /// <see cref="CreateIndexReq.Sync" /> is set, waits for that index to finish building before continuing.
    /// </summary>
    /// <param name="request">The request containing the collection name and the indexes to create.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task CreateIndexAsync(
        CreateIndexReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        if (request.Indexes.Count == 0)
        {
            throw new ArgumentException("CreateIndexReq.Indexes must not be empty.", nameof(request));
        }

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        foreach (IndexParam index in request.Indexes)
        {
            Grpc.CreateIndexRequest grpcRequest = request.ToGrpcCreateIndexRequest(index);
            await InvokeAsync(GrpcClient.CreateIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);

            if (request.Sync)
            {
                await WaitForIndexCompleteAsync(
                    request.CollectionName, index.FieldName, index.IndexName, request.TimeoutMs, request.DatabaseName, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    // Polls DescribeIndex until the created index reaches Finished (or None), fails on Failed, and throws on
    // timeout. Mirrors the Java SDK's WaitForIndexComplete.
    private async Task WaitForIndexCompleteAsync(
        string collectionName, string fieldName, string? indexName, long timeoutMs, string? databaseName,
        CancellationToken cancellationToken)
    {
        DateTime? deadline = timeoutMs > 0
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs)
            : null;

        // Pin the created index's name: the create request defaults an unset IndexName to "_default_idx", and
        // a poll with an empty IndexName would describe *all* indexes of the collection, so the first entry
        // might not be the index we just created.
        string pinnedName = string.IsNullOrEmpty(indexName) ? Constants.DefaultIndexName : indexName!;

        while (true)
        {
            DescribeIndexResp description = await DescribeIndexAsync(
                new DescribeIndexReq
                {
                    DatabaseName = databaseName,
                    CollectionName = collectionName,
                    FieldName = fieldName,
                    IndexName = pinnedName
                },
                cancellationToken).ConfigureAwait(false);

            // Multiple vector fields can share the default index name, so the returned list may contain indexes
            // for other fields; pick the one built on the requested field.
            IndexDesc? index = description.Indexes
                .FirstOrDefault(i => string.IsNullOrEmpty(i.FieldName) || i.FieldName == fieldName);
            if (index is null)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError,
                    $"Index on field '{fieldName}' of collection '{collectionName}' could not be described.");
            }

            if (index.State is IndexState.Finished or IndexState.None)
            {
                return;
            }

            if (index.State == IndexState.Failed)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError,
                    $"Index on field '{fieldName}' of collection '{collectionName}' failed to build"
                    + (string.IsNullOrEmpty(index.IndexStateFailReason) ? "" : $": {index.IndexStateFailReason}"));
            }

            if (deadline is not null && DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for the index on field '{fieldName}' to build.");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Drops an index.
    /// </summary>
    /// <param name="request">The request containing the collection/field names and the index name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task DropIndexAsync(
        DropIndexReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DropIndexRequest grpcRequest = request.ToGrpcDropIndexRequest();
        await InvokeAsync(GrpcClient.DropIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Describes the indexes on a field of a collection.
    /// </summary>
    /// <param name="request">The request containing the collection/field names and the index name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<DescribeIndexResp> DescribeIndexAsync(
        DescribeIndexReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DescribeIndexRequest grpcRequest = request.ToGrpcDescribeIndexRequest();
        Grpc.DescribeIndexResponse response = await InvokeAsync(
                GrpcClient.DescribeIndexAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return DescribeIndexResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists the indexes of a collection.
    /// </summary>
    /// <param name="request">The request containing the collection name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<ListIndexesResp> ListIndexesAsync(
        ListIndexesReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DescribeIndexRequest grpcRequest = request.ToGrpcDescribeIndexRequest();
        try
        {
            Grpc.DescribeIndexResponse response = await InvokeAsync(
                    GrpcClient.DescribeIndexAsync, grpcRequest, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

            ListIndexesResp result = ListIndexesResp.FromGrpc(response);

            // The proxy ignores field_name on DescribeIndex (it forwards only collection id and index name to
            // the datacoord), so filter the returned indexes client-side by FieldName, as the C++ SDK does.
            if (!string.IsNullOrEmpty(request.FieldName))
            {
                result = ListIndexesResp.WithIndexes(result.Indexes.Where(i => i.FieldName == request.FieldName).ToList());
            }

            return result;
        }
        catch (MilvusException ex) when (ex.ErrorCode == MilvusErrorCode.IndexNotFound)
        {
            // A collection with no index reports IndexNotFound (code 700); return an empty list instead of
            // surfacing the error, matching the C++ and Java SDKs.
            return new ListIndexesResp(Array.Empty<IndexDesc>());
        }
    }

    /// <summary>
    /// Alters the properties of an index.
    /// </summary>
    public async Task AlterIndexPropertiesAsync(AlterIndexPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterIndexRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops the properties of an index.
    /// </summary>
    public async Task DropIndexPropertiesAsync(DropIndexPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterIndexRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }
}
