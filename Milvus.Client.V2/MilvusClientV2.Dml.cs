using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Inserts rows into a collection, recording the mutation timestamp for Session consistency
    /// (see design doc §5.1.2).
    /// </summary>
    /// <param name="request">The request containing the collection name and field data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<MutationResp> InsertAsync(
        InsertReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.InsertRequest grpcRequest = request.ToGrpcInsertRequest();
        Grpc.MutationResult response = await InvokeAsync(
                GrpcClient.InsertAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        CollectionTsCache.Instance.Set(_endpoint, _database, request.CollectionName, unchecked((long)response.Timestamp));

        return MutationResp.FromGrpc(response);
    }

    /// <summary>
    /// Upserts (inserts or updates) rows into a collection, recording the mutation timestamp for Session
    /// consistency.
    /// </summary>
    /// <param name="request">The request containing the collection name and field data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<MutationResp> UpsertAsync(
        UpsertReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.UpsertRequest grpcRequest = request.ToGrpcUpsertRequest();
        Grpc.MutationResult response = await InvokeAsync(
                GrpcClient.UpsertAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        CollectionTsCache.Instance.Set(_endpoint, _database, request.CollectionName, unchecked((long)response.Timestamp));

        return MutationResp.FromGrpc(response);
    }

    /// <summary>
    /// Deletes rows from a collection by expression, recording the mutation timestamp for Session consistency.
    /// </summary>
    /// <param name="request">The request containing the collection name and delete expression.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<MutationResp> DeleteAsync(
        DeleteReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DeleteRequest grpcRequest = request.ToGrpcDeleteRequest();
        Grpc.MutationResult response = await InvokeAsync(
                GrpcClient.DeleteAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        CollectionTsCache.Instance.Set(_endpoint, _database, request.CollectionName, unchecked((long)response.Timestamp));

        return MutationResp.FromGrpc(response);
    }
}
