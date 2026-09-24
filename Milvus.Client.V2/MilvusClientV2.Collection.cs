using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a simple collection with a primary-key field and a float vector field, mirroring the C++ SDK's
    /// <c>CreateCollection(CreateSimpleCollectionRequest)</c>. An <see cref="IndexType.AutoIndex" /> index is
    /// created on the vector field and the collection is loaded automatically.
    /// </summary>
    /// <param name="request">The request containing the collection name and the simple schema options.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <example>
    /// <code>
    /// await client.CreateCollectionAsync(new CreateSimpleCollectionReq
    /// {
    ///     CollectionName = "book",
    ///     Dimension = 4
    /// });
    /// </code>
    /// </example>
    public Task CreateCollectionAsync(
        CreateSimpleCollectionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        if (request.Dimension < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.Dimension,
                "Dimension must be at least 2 (the Milvus proxy rejects dense-vector dimensions below 2).");
        }

        if (request.PrimaryFieldType is not DataType.Int64 and not DataType.VarChar)
        {
            throw new ArgumentException(
                "PrimaryFieldType must be Int64 or VarChar for a simple collection.", nameof(request));
        }

        if (request.PrimaryFieldType == DataType.VarChar && request.MaxLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.MaxLength,
                "MaxLength must be greater than zero when PrimaryFieldType is VarChar.");
        }

        var pkField = new FieldSchema(request.PrimaryFieldName, request.PrimaryFieldType,
            isPrimaryKey: true, autoId: request.AutoID);
        if (request.PrimaryFieldType == DataType.VarChar)
        {
            pkField.MaxLength = request.MaxLength;
        }

        var schema = new CollectionSchema
        {
            EnableDynamicFields = request.EnableDynamicFields,
            Fields =
            {
                pkField,
                new FieldSchema(request.VectorFieldName, DataType.FloatVector) { Dimension = request.Dimension }
            }
        };

        return CreateCollectionAsync(
            new CreateCollectionReq
            {
                CollectionName = request.CollectionName,
                DatabaseName = request.DatabaseName,
                Schema = schema,
                ConsistencyLevel = request.ConsistencyLevel,
                Indexes =
                [
                    new IndexParam(request.VectorFieldName, null, IndexType.AutoIndex, request.MetricType)
                ]
            },
            cancellationToken);
    }

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

        // The server may have a newer schema for this collection name than our cache (e.g. it was dropped and
        // recreated by another client); invalidate so the next describe/insert reflects the created schema.
        // Mirrors the Java SDK's invalidateSchemaCache on create.
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));

        if (request.Indexes.Count == 0)
        {
            return;
        }

        // Create the requested indexes immediately after the collection is created, then load it automatically,
        // mirroring the C++ and Java SDKs. Indexes are created asynchronously (Sync = false) so a create with
        // several indexes does not block up to TimeoutMs per index; the load below waits for them server-side.
        // Both sub-requests must target the same (request-level) database the collection was created in.
        await CreateIndexAsync(
            new CreateIndexReq
            {
                CollectionName = request.CollectionName,
                DatabaseName = request.DatabaseName,
                Indexes = request.Indexes,
                Sync = false
            },
            cancellationToken)
            .ConfigureAwait(false);

        await LoadCollectionAsync(
            new LoadCollectionReq
            {
                CollectionName = request.CollectionName,
                DatabaseName = request.DatabaseName,
                Sync = false
            },
            cancellationToken).ConfigureAwait(false);
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

        // A dropped collection no longer exists; drop it from the caches along with any alias-keyed entries
        // that describe it (design doc §5.1.2 / §5.1.3).
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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

        CollectionCacheKey key = CollectionCacheKey.Create(_endpoint, ResolveDatabaseName(request.DatabaseName), request.CollectionName);
        return await SchemaCache.Instance.GetOrLoadAsync(key, DescribeCollectionCore, cancellationToken: cancellationToken)
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

        Grpc.ShowCollectionsRequest grpcRequest = request.ToGrpcShowCollectionsRequest();
        Grpc.ShowCollectionsResponse response = await InvokeAsync(
                GrpcClient.ShowCollectionsAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return ListCollectionsResp.FromGrpc(response);
    }

    /// <summary>
    /// Describes multiple collections at once, by name and/or collection ID.
    /// </summary>
    /// <param name="request">The request containing the collection names and/or IDs to describe.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<IReadOnlyList<DescribeCollectionResp>> BatchDescribeCollectionsAsync(
        BatchDescribeCollectionsReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.BatchDescribeCollectionRequest grpcRequest = request.ToGrpcBatchDescribeCollectionRequest();
        Grpc.BatchDescribeCollectionResponse response = await InvokeAsync(
                GrpcClient.BatchDescribeCollectionAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        var results = new List<DescribeCollectionResp>(response.Responses.Count);
        foreach (Grpc.DescribeCollectionResponse collectionResponse in response.Responses)
        {
            // A requested collection that does not exist surfaces as a per-response error (code != 0) while the
            // top-level status stays OK. Surface it instead of crashing on the missing schema, matching the C++
            // SDK which returns SERVER_FAILED for the batch.
            if (collectionResponse.Status.Code != 0)
            {
                throw new MilvusException(
                    (MilvusErrorCode)collectionResponse.Status.Code,
                    string.IsNullOrEmpty(collectionResponse.Status.Reason)
                        ? $"Failed to describe a collection in the batch."
                        : collectionResponse.Status.Reason);
            }

            results.Add(DescribeCollectionResp.FromGrpc(collectionResponse));
        }

        return results;
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

        return GetCollectionStatsResp.FromGrpc(request.CollectionName, response);
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

        // A rename can also move the collection into another database (RenameCollectionRequest.NewDBName); the
        // cached session timestamp follows the collection to the target database, while the schema is
        // invalidated in both places so the next describe re-fetches it (mirroring the Java SDK, which moves
        // the ts cache but invalidates the schema cache). InvalidateCollectionCaches also drops alias-keyed
        // schema/ts entries that resolve to the old canonical name, so a later describe under an alias is not
        // served a stale pre-rename schema.
        string sourceDatabase = ResolveDatabaseName(request.DatabaseName);
        string targetDatabase = request.TargetDatabaseName ?? sourceDatabase;
        CollectionTsCache.Instance.Move(_endpoint, sourceDatabase, request.CollectionName, targetDatabase, request.NewCollectionName);
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
        SchemaCache.Instance.Invalidate(_endpoint, targetDatabase, request.NewCollectionName);
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

        if (!request.Sync)
        {
            return;
        }

        // Wait for the collection to be fully loaded, mirroring the C++ and Java SDKs (which poll
        // GetLoadState every 500 ms until Loaded or the timeout elapses).
        DateTime? deadline = request.TimeoutMs > 0
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(request.TimeoutMs)
            : null;

        while (true)
        {
            GetLoadStateResp state = await GetLoadStateAsync(
                new GetLoadStateReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName },
                cancellationToken).ConfigureAwait(false);

            if (state.State == LoadState.Loaded)
            {
                return;
            }

            if (state.State is LoadState.NotExist or LoadState.NotLoad)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError,
                    $"Collection '{request.CollectionName}' is in {state.State} state; load did not complete.");
            }

            if (deadline is not null && DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for collection '{request.CollectionName}' to load.");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
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
    /// Gets the load state of a collection, including the loading progress when the collection is loading.
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

        GetLoadStateResp result = GetLoadStateResp.FromGrpc(response);

        // Mirror the C++ SDK: report 100% when loaded, otherwise query the loading-progress RPC while loading.
        if (result.State == LoadState.Loaded)
        {
            result.Progress = 100;
        }
        else if (result.State == LoadState.Loading)
        {
            // Forward the request's partition names so a partition-scoped load reports accurate progress
            // (the whole-collection progress otherwise), matching the C++ SDK's GetLoadState.
            var progressRequest = new Grpc.GetLoadingProgressRequest
            {
                DbName = request.DatabaseName ?? "",
                CollectionName = request.CollectionName
            };
            progressRequest.PartitionNames.AddRange(request.PartitionNames);

            Grpc.GetLoadingProgressResponse progressResponse = await InvokeAsync(
                    GrpcClient.GetLoadingProgressAsync, progressRequest, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);
            result.Progress = progressResponse.Progress;
        }

        return result;
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

        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
    }

    /// <summary>
    /// Adds a function (e.g. BM25) to a collection's schema.
    /// </summary>
    public async Task AddCollectionFunctionAsync(AddCollectionFunctionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        DescribeCollectionResp description = await DescribeCollectionAsync(new DescribeCollectionReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName }, cancellationToken).ConfigureAwait(false);
        Grpc.AddCollectionFunctionRequest grpcRequest = request.ToGrpcAddCollectionFunctionRequest(description.CollectionId);
        await InvokeAsync(GrpcClient.AddCollectionFunctionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
    }

    /// <summary>
    /// Alters a function in a collection's schema.
    /// </summary>
    public async Task AlterCollectionFunctionAsync(AlterCollectionFunctionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        DescribeCollectionResp description = await DescribeCollectionAsync(new DescribeCollectionReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName }, cancellationToken).ConfigureAwait(false);
        Grpc.AlterCollectionFunctionRequest grpcRequest = request.ToGrpcAlterCollectionFunctionRequest(description.CollectionId);
        await InvokeAsync(GrpcClient.AlterCollectionFunctionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
    }

    /// <summary>
    /// Drops a function from a collection's schema.
    /// </summary>
    public async Task DropCollectionFunctionAsync(DropCollectionFunctionReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        DescribeCollectionResp description = await DescribeCollectionAsync(new DescribeCollectionReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName }, cancellationToken).ConfigureAwait(false);
        Grpc.DropCollectionFunctionRequest grpcRequest = request.ToGrpcDropCollectionFunctionRequest(description.CollectionId);
        await InvokeAsync(GrpcClient.DropCollectionFunctionAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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
        InvalidateCollectionCaches(request.CollectionName, ResolveDatabaseName(request.DatabaseName));
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

        DateTime? deadline = request.TimeoutMilliseconds is { } timeout && timeout > 0
            ? DateTime.UtcNow + TimeSpan.FromMilliseconds(timeout)
            : null;

        while (true)
        {
            GetLoadStateResp state = await GetLoadStateAsync(
                new GetLoadStateReq { DatabaseName = request.DatabaseName, CollectionName = request.CollectionName },
                cancellationToken).ConfigureAwait(false);

            // Fail fast on terminal non-loaded states (collection dropped mid-refresh, or loading rejected),
            // mirroring the C++ RefreshLoad which breaks out of its wait loop on a non-OK status.
            if (state.State is LoadState.NotExist or LoadState.NotLoad)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError,
                    $"Collection '{request.CollectionName}' is in {state.State} state; refresh-load did not complete.");
            }

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

    // Removes the given collection's schema and timestamp cache entries, plus every alias-keyed entry that
    // describes it. DescribeCollectionAsync caches under whatever name was used (including an alias), so a
    // drop or schema mutation issued via the canonical name must also drop the alias-keyed entries, and one
    // issued via an alias must also drop the canonical-name entry -- otherwise a later DescribeCollectionAsync
    // keeps serving the stale (or dead) schema with no RPC.
    private void InvalidateCollectionCaches(string collectionName, string databaseName)
    {
        // Resolve the canonical name *before* invalidating, since invalidating this key removes the mapping
        // that TryGetResolvedCollectionName reads below.
        SchemaCache.Instance.TryGetResolvedCollectionName(_endpoint, databaseName, collectionName, out string? canonical);

        SchemaCache.Instance.Invalidate(_endpoint, databaseName, collectionName);
        CollectionTsCache.Instance.Invalidate(_endpoint, databaseName, collectionName);

        // Canonical -> aliases: drop every alias-keyed entry whose cached schema resolves back to the
        // operation's collection name.
        foreach (string alias in SchemaCache.Instance.GetAliasKeys(_endpoint, databaseName, collectionName))
        {
            SchemaCache.Instance.Invalidate(_endpoint, databaseName, alias);
            CollectionTsCache.Instance.Invalidate(_endpoint, databaseName, alias);
        }

        // Alias -> canonical: when the operation itself used an alias name, drop the canonical-name entry too.
        if (canonical is not null)
        {
            SchemaCache.Instance.Invalidate(_endpoint, databaseName, canonical);
            CollectionTsCache.Instance.Invalidate(_endpoint, databaseName, canonical);
        }
    }
}