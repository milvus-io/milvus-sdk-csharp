using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Insert rows of data entities into a collection.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fields">Fields</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(fields);
        Verify.NotNullOrWhiteSpace(dbName);

        InsertRequest request = new()
        {
            CollectionName = collectionName,
            DbName = dbName
        };
        if (!string.IsNullOrEmpty(partitionName))
        {
            request.PartitionName = partitionName;
        }

        long count = fields[0].RowCount;
        for (int i = 1; i < fields.Count; i++)
        {
            if (fields[i].RowCount != count)
            {
                throw new ArgumentOutOfRangeException($"{nameof(fields)}[{i}])", "Fields length is not same");
            }
        }

        request.FieldsData.AddRange(fields.Select(static p => p.ToGrpcFieldData()));
        request.NumRows = (uint)count;

        MutationResult response = await InvokeAsync(_grpcClient.InsertAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusMutationResult.From(response);
    }

    /// <summary>
    /// Delete rows of data entities from a collection by given expression.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="expr">A predicate expression outputs a boolean value. <see href="https://milvus.io/docs/boolean.md"/></param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string? partitionName = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(expr);
        Verify.NotNullOrWhiteSpace(dbName);

        MutationResult response = await InvokeAsync(_grpcClient.DeleteAsync, new DeleteRequest
        {
            CollectionName = collectionName,
            Expr = expr,
            DbName = dbName,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusMutationResult.From(response);
    }

    /// <summary>
    /// Do a k nearest neighbors search with bool expression.
    /// </summary>
    /// <param name="searchParameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchParameters);
        searchParameters.Validate();

        SearchResults response = await InvokeAsync(_grpcClient.SearchAsync, searchParameters.BuildGrpc(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusSearchResult.From(response);
    }

    /// <summary>
    /// Flush a collection's data to disk. Milvus data will be auto flushed.
    /// Flush is only required when you want to get up to date entities numbers in statistics due to some internal mechanism.
    /// It will be removed in the future.
    /// </summary>
    /// <param name="collectionNames">Collection names.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        FlushRequest request = new()
        {
            DbName = dbName
        };
        request.CollectionNames.AddRange(collectionNames);

        FlushResponse response = await InvokeAsync(_grpcClient.FlushAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusFlushResult.From(response);
    }

    /// <summary>
    /// Returns sealed segments information of a collection.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        GetPersistentSegmentInfoResponse response = await InvokeAsync(_grpcClient.GetPersistentSegmentInfoAsync, new GetPersistentSegmentInfoRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    /// <summary>
    /// Get the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">Segment ids</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>If segments flushed.</returns>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        GetFlushStateRequest request = new();
        request.SegmentIDs.AddRange(segmentIds);

        GetFlushStateResponse response = await InvokeAsync(_grpcClient.GetFlushStateAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Flushed;
    }

    /// <summary>
    /// Do a explicit record query by given expression.
    /// For example when you want to query by primary key.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="expr"></param>
    /// <param name="outputFields"></param>
    /// <param name="consistencyLevel"></param>
    /// <param name="partitionNames">Partitions names.(Optional)</param>
    /// <param name="guaranteeTimestamp">
    /// guarantee_timestamp.
    /// (Optional)Instructs server to see insert/delete operations performed before a provided timestamp.
    /// If no such timestamp is specified, the server will wait for the latest operation to finish and query.
    /// </param>
    /// <param name="offset">
    /// offset a value to define the position.
    /// Specify a position to return results. Only take effect when the 'limit' value is specified.
    /// Default value is 0, start from begin.
    /// </param>
    /// <param name="limit">
    /// limit a value to define the limit of returned entities
    /// Specify a value to control the returned number of entities. Must be a positive value.
    /// Default value is 0, will return without limit.
    /// </param>
    /// <param name="travelTimestamp">Travel time.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<MilvusQueryResult> QueryAsync(
        string collectionName,
        string expr,
        IList<string> outputFields,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Bounded,
        IList<string>? partitionNames = null,
        long travelTimestamp = 0,
        long guaranteeTimestamp = Constants.GUARANTEE_EVENTUALLY_TS,
        long offset = 0,
        long limit = 0,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(outputFields);
        Verify.NotNullOrWhiteSpace(expr);
        Verify.GreaterThanOrEqualTo(guaranteeTimestamp, 0);
        Verify.GreaterThanOrEqualTo(travelTimestamp, 0);
        Verify.GreaterThanOrEqualTo(offset, 0);
        Verify.GreaterThanOrEqualTo(limit, 0);
        Verify.NotNullOrWhiteSpace(dbName);

        QueryRequest request = new()
        {
            CollectionName = collectionName,
            Expr = expr,
            GuaranteeTimestamp = (ulong)guaranteeTimestamp,
            TravelTimestamp = (ulong)travelTimestamp,
            DbName = dbName,
        };
        request.OutputFields.AddRange(outputFields);
        if (partitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }
        if (offset > 0)
        {
            Verify.GreaterThan(limit, 0);
            request.QueryParams.Add(new Grpc.KeyValuePair() { Key = "offset", Value = offset.ToString(CultureInfo.InvariantCulture) });
        }
        if (limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair() { Key = "limit", Value = limit.ToString(CultureInfo.InvariantCulture) });
        }

        QueryResults response = await InvokeAsync(_grpcClient.QueryAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusQueryResult.From(response);
    }

    /// <summary>
    /// Get query segment information.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="MilvusQuerySegmentInfoResult"/></returns>
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        GetQuerySegmentInfoResponse response = await InvokeAsync(_grpcClient.GetQuerySegmentInfoAsync, new GetQuerySegmentInfoRequest
        {
            CollectionName = collectionName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusQuerySegmentInfoResult.From(response).ToList();
    }
}
