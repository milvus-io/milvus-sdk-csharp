using IO.Milvus.Grpc;
using System.Globalization;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Insert rows of data entities into a collection.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fields">Fields</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName,
        IList<Field> fields,
        string partitionName = "",
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(fields);

        InsertRequest request = new() { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

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
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName,
        string expr,
        string? partitionName = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(expr);

        var request = new DeleteRequest
        {
            CollectionName = collectionName,
            Expr = expr,
            PartitionName = !string.IsNullOrEmpty(partitionName) ? partitionName : string.Empty
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        MutationResult response =
            await InvokeAsync(_grpcClient.DeleteAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusMutationResult.From(response);
    }

    /// <summary>
    /// Do a k nearest neighbors search with bool expression.
    /// </summary>
    /// <param name="searchParameters"></param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusSearchResult> SearchAsync(
        MilvusSearchParameters searchParameters,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchParameters);
        searchParameters.Validate();

        var request = searchParameters.BuildGrpc();

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        SearchResults response =
            await InvokeAsync(_grpcClient.SearchAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusSearchResult.From(response);
    }

    /// <summary>
    /// Flush a collection's data to disk. Milvus data will be auto flushed.
    /// Flush is only required when you want to get up to date entities numbers in statistics due to some internal mechanism.
    /// It will be removed in the future.
    /// </summary>
    /// <param name="collectionNames">Collection names.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusFlushResult> FlushAsync(
        IList<string> collectionNames,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);

        FlushRequest request = new();

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        request.CollectionNames.AddRange(collectionNames);

        FlushResponse response = await InvokeAsync(_grpcClient.FlushAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusFlushResult.From(response);
    }

    /// <summary>
    /// Returns sealed segments information of a collection.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusPersistentSegmentInfo>> GetPersistentSegmentInfosAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new GetPersistentSegmentInfoRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetPersistentSegmentInfoResponse response = await InvokeAsync(_grpcClient.GetPersistentSegmentInfoAsync,
            request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusPersistentSegmentInfo.From(response.Infos);
    }

    /// <summary>
    /// Get the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">Segment ids</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>If segments flushed.</returns>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        GetFlushStateRequest request = new();
        request.SegmentIDs.AddRange(segmentIds);

        GetFlushStateResponse response =
            await InvokeAsync(_grpcClient.GetFlushStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

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
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<MilvusQueryResult> QueryAsync(
        string collectionName,
        string expr,
        IList<string> outputFields,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.BoundedStaleness,
        IList<string>? partitionNames = null,
        long travelTimestamp = 0,
        long guaranteeTimestamp = Constants.GuaranteeEventuallyTs,
        long offset = 0,
        long limit = 0,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(outputFields);
        Verify.NotNullOrWhiteSpace(expr);
        Verify.GreaterThanOrEqualTo(guaranteeTimestamp, 0);
        Verify.GreaterThanOrEqualTo(travelTimestamp, 0);
        Verify.GreaterThanOrEqualTo(offset, 0);
        Verify.GreaterThanOrEqualTo(limit, 0);

        QueryRequest request = new()
        {
            CollectionName = collectionName,
            Expr = expr,
            GuaranteeTimestamp = (ulong)guaranteeTimestamp,
            TravelTimestamp = (ulong)travelTimestamp,
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        request.OutputFields.AddRange(outputFields);

        if (partitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        if (offset > 0)
        {
            Verify.GreaterThan(limit, 0);
            request.QueryParams.Add(new Grpc.KeyValuePair
            {
                Key = "offset", Value = offset.ToString(CultureInfo.InvariantCulture)
            });
        }

        if (limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair
            {
                Key = "limit", Value = limit.ToString(CultureInfo.InvariantCulture)
            });
        }

        QueryResults response =
            await InvokeAsync(_grpcClient.QueryAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusQueryResult.From(response);
    }

    /// <summary>
    /// Get query segment information.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns><see cref="MilvusQuerySegmentInfoResult"/></returns>
    public async Task<IList<MilvusQuerySegmentInfoResult>> GetQuerySegmentInfoAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new GetQuerySegmentInfoRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetQuerySegmentInfoResponse response =
            await InvokeAsync(_grpcClient.GetQuerySegmentInfoAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusQuerySegmentInfoResult.From(response).ToList();
    }
}
