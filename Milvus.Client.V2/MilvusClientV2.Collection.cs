using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="request">The request containing the collection name and schema.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task CreateCollectionAsync(
        CreateCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.CreateCollectionRequest grpcRequest = request.ToGrpcCreateCollectionRequest();
        await InvokeAsync(GrpcClient.CreateCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a collection.
    /// </summary>
    /// <param name="request">The request containing the name of the collection to drop.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task DropCollectionAsync(
        DropCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DropCollectionRequest grpcRequest = request.ToGrpcDropCollectionRequest();
        await InvokeAsync(GrpcClient.DropCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);

        // A dropped collection no longer exists; drop it from the caches (design doc §5.1.2 / §5.1.3).
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
        CollectionTsCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Checks whether a collection exists.
    /// </summary>
    /// <param name="request">The request containing the name of the collection to check.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<HasCollectionResp> HasCollectionAsync(
        HasCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.HasCollectionRequest grpcRequest = request.ToGrpcHasCollectionRequest();
        Grpc.BoolResponse response = await InvokeAsync(
                GrpcClient.HasCollectionAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return HasCollectionResp.FromGrpc(response);
    }

    /// <summary>
    /// Describes a collection, returning its schema. The schema is served from <see cref="SchemaCache" /> when
    /// available (see design doc §5.1.3).
    /// </summary>
    /// <param name="request">The request containing the name of the collection to describe.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<DescribeCollectionResp> DescribeCollectionAsync(
        DescribeCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        CollectionCacheKey key = CollectionCacheKey.Create(_endpoint, _database, request.CollectionName);
        return await SchemaCache.Instance.GetOrLoadAsync(key, DescribeCollectionCore, cancellationToken)
            .ConfigureAwait(false);

        async ValueTask<DescribeCollectionResp> DescribeCollectionCore(CancellationToken ct)
        {
            Grpc.DescribeCollectionRequest grpcRequest = request.ToGrpcDescribeCollectionRequest();
            Grpc.DescribeCollectionResponse response = await InvokeAsync(
                    GrpcClient.DescribeCollectionAsync, grpcRequest, static r => r.Status, ct)
                .ConfigureAwait(false);

            return DescribeCollectionResp.FromGrpc(response);
        }
    }

    /// <summary>
    /// Lists all collections in the database.
    /// </summary>
    /// <param name="request">The request parameters.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<ListCollectionsResp> ListCollectionsAsync(
        ListCollectionsReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.ShowCollectionsRequest grpcRequest = ListCollectionsReq.ToGrpcShowCollectionsRequest();
        Grpc.ShowCollectionsResponse response = await InvokeAsync(
                GrpcClient.ShowCollectionsAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return ListCollectionsResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the statistics of a collection.
    /// </summary>
    /// <param name="request">The request containing the collection name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GetCollectionStatsResp> GetCollectionStatsAsync(
        GetCollectionStatsReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.GetCollectionStatisticsRequest grpcRequest = request.ToGrpcGetCollectionStatisticsRequest();
        Grpc.GetCollectionStatisticsResponse response = await InvokeAsync(
                GrpcClient.GetCollectionStatisticsAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return GetCollectionStatsResp.FromGrpc(response);
    }

    /// <summary>
    /// Renames a collection, transferring the cached session timestamp (see design doc §5.1.2).
    /// </summary>
    /// <param name="request">The request containing the current and new collection names.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task RenameCollectionAsync(
        RenameCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.RenameCollectionRequest grpcRequest = request.ToGrpcRenameCollectionRequest();
        await InvokeAsync(GrpcClient.RenameCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);

        CollectionTsCache.Instance.Move(_endpoint, _database, request.CollectionName, _database, request.NewCollectionName);
    }

    /// <summary>
    /// Loads a collection into memory.
    /// </summary>
    /// <param name="request">The request containing the collection name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task LoadCollectionAsync(
        LoadCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.LoadCollectionRequest grpcRequest = request.ToGrpcLoadCollectionRequest();
        await InvokeAsync(GrpcClient.LoadCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases a loaded collection from memory.
    /// </summary>
    /// <param name="request">The request containing the collection name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task ReleaseCollectionAsync(
        ReleaseCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.ReleaseCollectionRequest grpcRequest = request.ToGrpcReleaseCollectionRequest();
        await InvokeAsync(GrpcClient.ReleaseCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the load state of a collection.
    /// </summary>
    /// <param name="request">The request containing the collection name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GetLoadStateResp> GetLoadStateAsync(
        GetLoadStateReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.GetLoadStateRequest grpcRequest = request.ToGrpcGetLoadStateRequest();
        Grpc.GetLoadStateResponse response = await InvokeAsync(
                GrpcClient.GetLoadStateAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return GetLoadStateResp.FromGrpc(response);
    }

    /// <summary>
    /// Adds a field to a collection's schema, invalidating the cached schema.
    /// </summary>
    /// <param name="request">The request containing the collection name and the field to add.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task AddCollectionFieldAsync(
        AddCollectionFieldReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.AddCollectionFieldRequest grpcRequest = request.ToGrpcAddCollectionFieldRequest();
        await InvokeAsync(GrpcClient.AddCollectionFieldAsync, grpcRequest, cancellationToken).ConfigureAwait(false);

        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Adds a function (e.g. BM25) to a collection's schema.
    /// </summary>
    public async Task AddCollectionFunctionAsync(AddCollectionFunctionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        DescribeCollectionResp description = await DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = request.CollectionName }, cancellationToken).ConfigureAwait(false);
        Grpc.AddCollectionFunctionRequest grpcRequest = request.ToGrpcAddCollectionFunctionRequest(description.CollectionId);
        await InvokeAsync(GrpcClient.AddCollectionFunctionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Alters a function in a collection's schema.
    /// </summary>
    public async Task AlterCollectionFunctionAsync(AlterCollectionFunctionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        DescribeCollectionResp description = await DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = request.CollectionName }, cancellationToken).ConfigureAwait(false);
        Grpc.AlterCollectionFunctionRequest grpcRequest = request.ToGrpcAlterCollectionFunctionRequest(description.CollectionId);
        await InvokeAsync(GrpcClient.AlterCollectionFunctionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Drops a function from a collection's schema.
    /// </summary>
    public async Task DropCollectionFunctionAsync(DropCollectionFunctionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        DescribeCollectionResp description = await DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = request.CollectionName }, cancellationToken).ConfigureAwait(false);
        Grpc.DropCollectionFunctionRequest grpcRequest = request.ToGrpcDropCollectionFunctionRequest(description.CollectionId);
        await InvokeAsync(GrpcClient.DropCollectionFunctionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Alters a field of a collection's schema.
    /// </summary>
    public async Task AlterCollectionFieldAsync(AlterCollectionFieldReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterCollectionFieldRequest grpcRequest = request.ToGrpcAlterCollectionFieldRequest();
        await InvokeAsync(GrpcClient.AlterCollectionFieldAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Alters the properties of a collection.
    /// </summary>
    public async Task AlterCollectionPropertiesAsync(AlterCollectionPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterCollectionRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops the properties of a collection.
    /// </summary>
    public async Task DropCollectionPropertiesAsync(DropCollectionPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterCollectionRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops the properties of a field in a collection's schema.
    /// </summary>
    public async Task DropCollectionFieldPropertiesAsync(DropCollectionFieldPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterCollectionFieldRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterCollectionFieldAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Describes the replicas of a loaded collection.
    /// </summary>
    public async Task<DescribeReplicasResp> DescribeReplicasAsync(DescribeReplicasReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetReplicasRequest grpcRequest = request.ToGrpcRequest();
        Grpc.GetReplicasResponse response = await InvokeAsync(
            GrpcClient.GetReplicasAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeReplicasResp.FromGrpc(response);
    }

    /// <summary>
    /// Removes all entities from a collection.
    /// </summary>
    public async Task TruncateCollectionAsync(TruncateCollectionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.TruncateCollectionRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(
            GrpcClient.TruncateCollectionAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        SchemaCache.Instance.Invalidate(_endpoint, _database, request.CollectionName);
    }

    /// <summary>
    /// Refreshes the loaded data of a collection.
    /// </summary>
    /// <param name="request">The refresh-load request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task RefreshLoadAsync(RefreshLoadReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        request.Validate();
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.LoadCollectionRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.LoadCollectionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);

        if (!request.Sync)
        {
            return;
        }

        DateTime? deadline = request.TimeoutMilliseconds is { } timeout
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout)
            : null;

        while (true)
        {
            GetLoadStateResp state = await GetLoadStateAsync(
                new GetLoadStateReq { CollectionName = request.CollectionName },
                cancellationToken).ConfigureAwait(false);
            if (state.State == LoadState.Loaded)
            {
                return;
            }

            if (deadline is not null && DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for collection '{request.CollectionName}' to reload.");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
    }
}