using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Aliases;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Database;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class CollectionApiTests
{
    [Fact]
    public async Task CreateCollection_forwards_request_and_succeeds()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = "coll",
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            }
        }, TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastCreatedCollectionName);
    }

    [Fact]
    public async Task CreateCollection_with_indexes_forwards_database_to_sub_requests()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = "coll",
            DatabaseName = "db_override",
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            },
            Indexes = [new IndexParam("embedding", null, IndexType.Flat, SimilarityMetricType.L2)]
        }, TestContext.Current.CancellationToken);

        // The create carries the database, and so must the auto-created index and auto-load sub-requests.
        Grpc.CreateCollectionRequest create =
            Assert.IsType<Grpc.CreateCollectionRequest>(server.Service.Requests["CreateCollection"]);
        Assert.Equal("db_override", create.DbName);

        Grpc.CreateIndexRequest index =
            Assert.IsType<Grpc.CreateIndexRequest>(server.Service.Requests["CreateIndex"]);
        Assert.Equal("db_override", index.DbName);

        Grpc.LoadCollectionRequest load =
            Assert.IsType<Grpc.LoadCollectionRequest>(server.Service.Requests["LoadCollection"]);
        Assert.Equal("db_override", load.DbName);
    }

    [Fact]
    public async Task HasCollection_maps_response()
    {
        using var server = new MockMilvusServer { Service = { HasCollectionResult = true } };
        using MilvusClientV2 client = server.CreateClient();

        HasCollectionResp response =
            await client.HasCollectionAsync(new HasCollectionReq { CollectionName = "coll" }, TestContext.Current.CancellationToken);

        Assert.True(response.Has);
        Assert.Equal("coll", server.Service.LastCheckedCollectionName);
    }

    [Fact]
    public async Task ListCollections_maps_response()
    {
        using var server = new MockMilvusServer();
        server.Service.CollectionNames.Add("a");
        server.Service.CollectionNames.Add("b");
        using MilvusClientV2 client = server.CreateClient();

        ListCollectionsResp response = await client.ListCollectionsAsync(new ListCollectionsReq(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "a", "b" }, response.CollectionNames);
    }

    [Fact]
    public async Task Server_error_maps_to_MilvusException()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // Connect successfully first (lazy), then make the operation itself fail.
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        server.Service.FailureStatus = new Milvus.Client.Grpc.Status
        {
            Code = (int)MilvusErrorCode.CollectionNotFound,
            Reason = "collection not found"
        };

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            client.HasCollectionAsync(new HasCollectionReq { CollectionName = "missing" }, TestContext.Current.CancellationToken));

        Assert.Equal(MilvusErrorCode.CollectionNotFound, exception.ErrorCode);
        Assert.Contains("collection not found", exception.Message);
    }
}

/// <summary>
/// Verifies that the MilvusClientV2 collection-domain APIs forward the expected gRPC requests.
/// </summary>
[Trait("Category", "Integration")]
public class CollectionApiForwardingTests
{
    private static CollectionSchema PrimaryKeySchema()
        => new() { Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) } };

    [Fact]
    public async Task AddCollectionField_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        FieldSchema embedding = FieldSchema.CreateFloatVector("embedding", dimension: 8);
        embedding.Nullable = true;
        await client.AddCollectionFieldAsync(
            new AddCollectionFieldReq
            {
                CollectionName = "add_field_coll",
                Field = embedding
            },
            TestContext.Current.CancellationToken);

        Grpc.AddCollectionFieldRequest request =
            Assert.IsType<Grpc.AddCollectionFieldRequest>(server.Service.Requests["AddCollectionField"]);
        Assert.Equal("add_field_coll", request.CollectionName);

        Grpc.FieldSchema field = Grpc.FieldSchema.Parser.ParseFrom(request.Schema);
        Assert.Equal("embedding", field.Name);
        Assert.Equal(Grpc.DataType.FloatVector, field.DataType);
    }

    [Fact]
    public async Task AddCollectionFunction_forwards_request()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = PrimaryKeySchema();
        using MilvusClientV2 client = server.CreateClient();

        await client.AddCollectionFunctionAsync(
            new AddCollectionFunctionReq
            {
                CollectionName = "add_func_coll",
                Function = FunctionSchema.CreateBm25("bm25_func", "text", "sparse")
            },
            TestContext.Current.CancellationToken);

        Grpc.AddCollectionFunctionRequest request =
            Assert.IsType<Grpc.AddCollectionFunctionRequest>(server.Service.Requests["AddCollectionFunction"]);
        Assert.Equal("add_func_coll", request.CollectionName);
        Assert.Equal("bm25_func", request.FunctionSchema.Name);
    }

    [Fact]
    public async Task AlterCollectionField_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterCollectionFieldAsync(
            new AlterCollectionFieldReq
            {
                CollectionName = "alter_field_coll",
                FieldName = "embedding",
                Properties = { ["index_type"] = "IVF_FLAT" }
            },
            TestContext.Current.CancellationToken);

        Grpc.AlterCollectionFieldRequest request =
            Assert.IsType<Grpc.AlterCollectionFieldRequest>(server.Service.Requests["AlterCollectionField"]);
        Assert.Equal("alter_field_coll", request.CollectionName);
        Assert.Equal("embedding", request.FieldName);
        Assert.Equal("index_type", request.Properties.Single().Key);
        Assert.Equal("IVF_FLAT", request.Properties.Single().Value);
    }

    [Fact]
    public async Task AlterCollectionFunction_forwards_request()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = PrimaryKeySchema();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterCollectionFunctionAsync(
            new AlterCollectionFunctionReq
            {
                CollectionName = "alter_func_coll",
                FunctionName = "bm25_func",
                Function = FunctionSchema.CreateBm25("bm25_func", "text", "sparse")
            },
            TestContext.Current.CancellationToken);

        Grpc.AlterCollectionFunctionRequest request =
            Assert.IsType<Grpc.AlterCollectionFunctionRequest>(server.Service.Requests["AlterCollectionFunction"]);
        Assert.Equal("alter_func_coll", request.CollectionName);
        Assert.Equal("bm25_func", request.FunctionName);
        Assert.Equal("bm25_func", request.FunctionSchema.Name);
    }

    [Fact]
    public async Task AlterCollectionProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterCollectionPropertiesAsync(
            new AlterCollectionPropertiesReq
            {
                CollectionName = "alter_props_coll",
                Properties = { ["collection.ttl.seconds"] = "86400" }
            },
            TestContext.Current.CancellationToken);

        Grpc.AlterCollectionRequest request =
            Assert.IsType<Grpc.AlterCollectionRequest>(server.Service.Requests["AlterCollection"]);
        Assert.Equal("alter_props_coll", request.CollectionName);
        Assert.Equal("collection.ttl.seconds", request.Properties.Single().Key);
        Assert.Equal("86400", request.Properties.Single().Value);
    }

    [Fact]
    public async Task DescribeCollection_forwards_request()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "describe_coll",
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) }
        };
        using MilvusClientV2 client = server.CreateClient();

        DescribeCollectionResp response = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = "describe_coll" },
            TestContext.Current.CancellationToken);

        Grpc.DescribeCollectionRequest request =
            Assert.IsType<Grpc.DescribeCollectionRequest>(server.Service.Requests["DescribeCollection"]);
        Assert.Equal("describe_coll", request.CollectionName);
        Assert.Equal("describe_coll", response.CollectionName);
        Assert.Single(response.Schema.Fields);
    }

    [Fact]
    public async Task DescribeCollection_forwards_request_level_database_and_keys_cache_by_it()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "describe_coll",
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) }
        };
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();

        // First describe with a request-level database override issues the RPC against that database.
        await client.DescribeCollectionAsync(
            new DescribeCollectionReq { DatabaseName = "db_override", CollectionName = "describe_coll" },
            TestContext.Current.CancellationToken);

        Grpc.DescribeCollectionRequest first =
            Assert.IsType<Grpc.DescribeCollectionRequest>(server.Service.Requests["DescribeCollection"]);
        Assert.Equal("db_override", first.DbName);
        Assert.Equal(1, server.Service.DescribeCollectionCount);

        // A second describe on the same (database, collection) key is served from the cache: no extra RPC.
        await client.DescribeCollectionAsync(
            new DescribeCollectionReq { DatabaseName = "db_override", CollectionName = "describe_coll" },
            TestContext.Current.CancellationToken);
        Assert.Equal(1, server.Service.DescribeCollectionCount);

        // A different database is a different cache key, so the RPC fires again with the new database.
        await client.DescribeCollectionAsync(
            new DescribeCollectionReq { DatabaseName = "another_db", CollectionName = "describe_coll" },
            TestContext.Current.CancellationToken);
        Assert.Equal(2, server.Service.DescribeCollectionCount);
        Grpc.DescribeCollectionRequest last =
            Assert.IsType<Grpc.DescribeCollectionRequest>(server.Service.Requests["DescribeCollection"]);
        Assert.Equal("another_db", last.DbName);
    }

    [Fact]
    public async Task DescribeCollection_roundtrips_array_and_default_value_field_attributes()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = new CollectionSchema
        {
            Name = "describe_coll",
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("tags", DataType.Array)
                {
                    ElementDataType = DataType.Int64,
                    MaxCapacity = 8,
                    Nullable = true
                },
                new FieldSchema("title", DataType.VarChar) { MaxLength = 100, DefaultValue = "N/A" }
            }
        };
        using MilvusClientV2 client = server.CreateClient();

        DescribeCollectionResp response = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = "describe_coll" },
            TestContext.Current.CancellationToken);

        FieldSchema tags = response.Schema.Fields.First(f => f.Name == "tags");
        Assert.Equal(DataType.Int64, tags.ElementDataType);
        Assert.Equal(8, tags.MaxCapacity);
        Assert.True(tags.Nullable);

        FieldSchema title = response.Schema.Fields.First(f => f.Name == "title");
        Assert.Equal(100, title.MaxLength);
        Assert.Equal("N/A", title.DefaultValue);
    }

    [Fact]
    public async Task BatchDescribeCollections_forwards_names_and_maps_descriptions()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        IReadOnlyList<DescribeCollectionResp> response = await client.BatchDescribeCollectionsAsync(
            new BatchDescribeCollectionsReq { CollectionNames = new[] { "a", "b" } },
            TestContext.Current.CancellationToken);

        Grpc.BatchDescribeCollectionRequest request =
            Assert.IsType<Grpc.BatchDescribeCollectionRequest>(server.Service.Requests["BatchDescribeCollection"]);
        Assert.Equal(new[] { "a", "b" }, request.CollectionName);
        Assert.Equal(2, response.Count);
        Assert.Equal("a", response[0].CollectionName);
        Assert.Equal("b", response[1].CollectionName);
    }

    [Fact]
    public async Task BatchDescribeCollections_throws_on_missing_collection_response()
    {
        using var server = new MockMilvusServer();
        server.Service.BatchDescribeMissingCollections = new HashSet<string> { "missing" };
        using MilvusClientV2 client = server.CreateClient();

        MilvusException ex = await Assert.ThrowsAsync<MilvusException>(() =>
            client.BatchDescribeCollectionsAsync(
                new BatchDescribeCollectionsReq { CollectionNames = new[] { "ok", "missing" } },
                TestContext.Current.CancellationToken));

        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task BatchDescribeCollections_throws_when_empty()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.BatchDescribeCollectionsAsync(new BatchDescribeCollectionsReq(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DescribeReplicas_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        DescribeReplicasResp response = await client.DescribeReplicasAsync(
            new DescribeReplicasReq { CollectionName = "describe_replicas_coll", WithShardNodes = true },
            TestContext.Current.CancellationToken);

        Grpc.GetReplicasRequest request =
            Assert.IsType<Grpc.GetReplicasRequest>(server.Service.Requests["GetReplicas"]);
        Assert.Equal("describe_replicas_coll", request.CollectionName);
        Assert.True(request.WithShardNodes);
        Assert.Empty(response.Replicas);
    }

    [Fact]
    public async Task DropCollection_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropCollectionAsync(
            new DropCollectionReq { CollectionName = "drop_coll" },
            TestContext.Current.CancellationToken);

        Grpc.DropCollectionRequest request =
            Assert.IsType<Grpc.DropCollectionRequest>(server.Service.Requests["DropCollection"]);
        Assert.Equal("drop_coll", request.CollectionName);
    }

    [Fact]
    public async Task DropCollectionFieldProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropCollectionFieldPropertiesAsync(
            new DropCollectionFieldPropertiesReq
            {
                CollectionName = "drop_field_props_coll",
                FieldName = "embedding",
                DeleteKeys = new[] { "index_type" }
            },
            TestContext.Current.CancellationToken);

        Grpc.AlterCollectionFieldRequest request =
            Assert.IsType<Grpc.AlterCollectionFieldRequest>(server.Service.Requests["AlterCollectionField"]);
        Assert.Equal("drop_field_props_coll", request.CollectionName);
        Assert.Equal("embedding", request.FieldName);
        Assert.Equal(new[] { "index_type" }, request.DeleteKeys);
    }

    [Fact]
    public async Task DropCollectionFunction_forwards_request()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = PrimaryKeySchema();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropCollectionFunctionAsync(
            new DropCollectionFunctionReq
            {
                CollectionName = "drop_func_coll",
                FunctionName = "bm25_func"
            },
            TestContext.Current.CancellationToken);

        Grpc.DropCollectionFunctionRequest request =
            Assert.IsType<Grpc.DropCollectionFunctionRequest>(server.Service.Requests["DropCollectionFunction"]);
        Assert.Equal("drop_func_coll", request.CollectionName);
        Assert.Equal("bm25_func", request.FunctionName);
    }

    [Fact]
    public async Task DropCollectionProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropCollectionPropertiesAsync(
            new DropCollectionPropertiesReq
            {
                CollectionName = "drop_props_coll",
                DeleteKeys = new[] { "collection.ttl.seconds" }
            },
            TestContext.Current.CancellationToken);

        Grpc.AlterCollectionRequest request =
            Assert.IsType<Grpc.AlterCollectionRequest>(server.Service.Requests["AlterCollection"]);
        Assert.Equal("drop_props_coll", request.CollectionName);
        Assert.Equal(new[] { "collection.ttl.seconds" }, request.DeleteKeys);
    }

    [Fact]
    public async Task GetCollectionStats_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetCollectionStatsResp response = await client.GetCollectionStatsAsync(
            new GetCollectionStatsReq { CollectionName = "get_stats_coll" },
            TestContext.Current.CancellationToken);

        Grpc.GetCollectionStatisticsRequest request =
            Assert.IsType<Grpc.GetCollectionStatisticsRequest>(server.Service.Requests["GetCollectionStatistics"]);
        Assert.Equal("get_stats_coll", request.CollectionName);
        Assert.Equal(0L, response.RowCount);
    }

    [Fact]
    public async Task GetLoadState_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetLoadStateResp response = await client.GetLoadStateAsync(
            new GetLoadStateReq { CollectionName = "get_load_state_coll" },
            TestContext.Current.CancellationToken);

        Grpc.GetLoadStateRequest request =
            Assert.IsType<Grpc.GetLoadStateRequest>(server.Service.Requests["GetLoadState"]);
        Assert.Equal("get_load_state_coll", request.CollectionName);
        Assert.Equal(LoadState.Loaded, response.State);
        Assert.Equal(100, response.Progress);   // Loaded => 100%, matching C++/Java
    }

    [Fact]
    public async Task GetLoadState_reports_loading_progress()
    {
        using var server = new MockMilvusServer { Service = { GetLoadStateResult = Grpc.LoadState.Loading, LoadingProgress = 37 } };
        using MilvusClientV2 client = server.CreateClient();

        GetLoadStateResp response = await client.GetLoadStateAsync(
            new GetLoadStateReq { CollectionName = "get_load_state_coll" },
            TestContext.Current.CancellationToken);

        Assert.Equal(LoadState.Loading, response.State);
        Assert.Equal(37, response.Progress);
        Assert.Contains("GetLoadingProgress", server.Service.Requests.Keys);
    }

    [Fact]
    public async Task GetLoadState_forwards_partition_names_to_loading_progress()
    {
        using var server = new MockMilvusServer { Service = { GetLoadStateResult = Grpc.LoadState.Loading } };
        using MilvusClientV2 client = server.CreateClient();

        await client.GetLoadStateAsync(
            new GetLoadStateReq
            {
                CollectionName = "get_load_state_coll",
                PartitionNames = new[] { "p1", "p2" }
            },
            TestContext.Current.CancellationToken);

        var progressRequest = Assert.IsType<Grpc.GetLoadingProgressRequest>(server.Service.Requests["GetLoadingProgress"]);
        Assert.Equal(new[] { "p1", "p2" }, progressRequest.PartitionNames);
    }

    [Fact]
    public async Task LoadCollection_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.LoadCollectionAsync(
            new LoadCollectionReq { CollectionName = "load_coll", ReplicaNumber = 2 },
            TestContext.Current.CancellationToken);

        Grpc.LoadCollectionRequest request =
            Assert.IsType<Grpc.LoadCollectionRequest>(server.Service.Requests["LoadCollection"]);
        Assert.Equal("load_coll", request.CollectionName);
        Assert.Equal(2, request.ReplicaNumber);
        Assert.False(request.Refresh);
    }

    [Fact]
    public async Task RefreshLoad_forwards_refresh_flag()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RefreshLoadAsync(
            new RefreshLoadReq { CollectionName = "refresh_load_coll", Sync = false },
            TestContext.Current.CancellationToken);

        Grpc.LoadCollectionRequest request =
            Assert.IsType<Grpc.LoadCollectionRequest>(server.Service.Requests["LoadCollection"]);
        Assert.Equal("refresh_load_coll", request.CollectionName);
        Assert.True(request.Refresh);
    }

    [Fact]
    public async Task ReleaseCollection_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ReleaseCollectionAsync(
            new ReleaseCollectionReq { CollectionName = "release_coll" },
            TestContext.Current.CancellationToken);

        Grpc.ReleaseCollectionRequest request =
            Assert.IsType<Grpc.ReleaseCollectionRequest>(server.Service.Requests["ReleaseCollection"]);
        Assert.Equal("release_coll", request.CollectionName);
    }

    [Fact]
    public async Task RenameCollection_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RenameCollectionAsync(
            new RenameCollectionReq { CollectionName = "rename_coll", NewCollectionName = "rename_coll_new" },
            TestContext.Current.CancellationToken);

        Grpc.RenameCollectionRequest request =
            Assert.IsType<Grpc.RenameCollectionRequest>(server.Service.Requests["RenameCollection"]);
        Assert.Equal("rename_coll", request.OldName);
        Assert.Equal("rename_coll_new", request.NewName);
    }

    [Fact]
    public async Task RenameCollection_moves_ts_cache_to_target_database()
    {
        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();
        try
        {
            using var server = new MockMilvusServer();
            using MilvusClientV2 client = server.CreateClient();

            // Simulate a prior DML so the ts cache is populated for the source collection.
            CollectionTsCache.Instance.Set(server.Uri, "default", "rename_coll", 100);

            await client.RenameCollectionAsync(
                new RenameCollectionReq
                {
                    CollectionName = "rename_coll",
                    NewCollectionName = "rename_coll_new",
                    TargetDatabaseName = "db2"
                },
                TestContext.Current.CancellationToken);

            Assert.Equal(100L, CollectionTsCache.Instance.Get(server.Uri, "db2", "rename_coll_new"));
            Assert.Equal(0L, CollectionTsCache.Instance.Get(server.Uri, "default", "rename_coll"));
        }
        finally
        {
            CollectionTsCache.Instance.Clear();
            SchemaCache.Instance.Clear();
        }
    }

    [Fact]
    public async Task DropCollection_invalidates_schema_and_ts_caches()
    {
        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();
        try
        {
            using var server = new MockMilvusServer();
            using MilvusClientV2 client = server.CreateClient();

            server.Service.DescribeSchema = new CollectionSchema { Name = "drop_coll" };
            server.Service.DescribeSchema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));

            // Populate both caches via a describe (schema) and a prior DML (ts).
            await client.DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = "drop_coll" },
                TestContext.Current.CancellationToken);
            CollectionTsCache.Instance.Set(server.Uri, "default", "drop_coll", 100);

            // Cache an alias-keyed entry describing the same collection (describe-through-alias pattern).
            server.Service.DescribeSchema.Name = "drop_coll_alias";
            await client.DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = "drop_coll_alias" },
                TestContext.Current.CancellationToken);

            await client.DropCollectionAsync(new DropCollectionReq { CollectionName = "drop_coll" },
                TestContext.Current.CancellationToken);

            // The drop invalidates the canonical and the alias-keyed schema entries: a re-describe must hit the
            // server again instead of serving stale cache (count would stay 2 on a cache hit).
            await client.DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = "drop_coll" },
                TestContext.Current.CancellationToken);
            Assert.Equal(3, server.Service.DescribeCollectionCount);

            // Both the canonical and the alias-keyed ts entries are gone.
            Assert.Equal(0L, CollectionTsCache.Instance.Get(server.Uri, "default", "drop_coll"));
            Assert.Equal(0L, CollectionTsCache.Instance.Get(server.Uri, "default", "drop_coll_alias"));
        }
        finally
        {
            CollectionTsCache.Instance.Clear();
            SchemaCache.Instance.Clear();
        }
    }

    [Fact]
    public async Task Alias_ops_invalidate_then_copy_ts_cache()
    {
        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();
        try
        {
            using var server = new MockMilvusServer();
            using MilvusClientV2 client = server.CreateClient();

            CollectionTsCache.Instance.Set(server.Uri, "default", "base_coll", 100);
            CollectionTsCache.Instance.Set(server.Uri, "default", "other_coll", 200);

            // CreateAlias copies the target's ts to the alias and clears any stale prior binding.
            CollectionTsCache.Instance.Set(server.Uri, "default", "my_alias", 999);   // stale far-future value
            await client.CreateAliasAsync(new CreateAliasReq { Alias = "my_alias", CollectionName = "base_coll" },
                TestContext.Current.CancellationToken);
            Assert.Equal(100L, CollectionTsCache.Instance.Get(server.Uri, "default", "my_alias"));

            // AlterAlias re-points the alias: stale ts is cleared, then copied from the new target.
            await client.AlterAliasAsync(new AlterAliasReq { Alias = "my_alias", CollectionName = "other_coll" },
                TestContext.Current.CancellationToken);
            Assert.Equal(200L, CollectionTsCache.Instance.Get(server.Uri, "default", "my_alias"));

            // DropAlias removes the alias ts entirely.
            await client.DropAliasAsync(new DropAliasReq { Alias = "my_alias" },
                TestContext.Current.CancellationToken);
            Assert.Equal(0L, CollectionTsCache.Instance.Get(server.Uri, "default", "my_alias"));
        }
        finally
        {
            CollectionTsCache.Instance.Clear();
            SchemaCache.Instance.Clear();
        }
    }

    [Fact]
    public async Task DropDatabase_invalidates_db_caches()
    {
        CollectionTsCache.Instance.Clear();
        SchemaCache.Instance.Clear();
        try
        {
            using var server = new MockMilvusServer();
            using MilvusClientV2 client = server.CreateClient();

            server.Service.DescribeSchema = new CollectionSchema { Name = "coll_in_db2" };
            server.Service.DescribeSchema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));

            // Populate the schema cache for a collection in a non-default database.
            await client.DescribeCollectionAsync(new DescribeCollectionReq
            {
                CollectionName = "coll_in_db2",
                DatabaseName = "db2"
            }, TestContext.Current.CancellationToken);
            int schemaCountBefore = SchemaCache.Instance.Count;
            CollectionTsCache.Instance.Set(server.Uri, "db2", "coll_in_db2", 100);

            await client.DropDatabaseAsync(new DropDatabaseReq { DatabaseName = "db2" },
                TestContext.Current.CancellationToken);

            Assert.Equal(0L, CollectionTsCache.Instance.Get(server.Uri, "db2", "coll_in_db2"));
            Assert.Equal(schemaCountBefore - 1, SchemaCache.Instance.Count);
        }
        finally
        {
            CollectionTsCache.Instance.Clear();
            SchemaCache.Instance.Clear();
        }
    }

    [Fact]
    public async Task TruncateCollection_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.TruncateCollectionAsync(
            new TruncateCollectionReq { CollectionName = "truncate_coll" },
            TestContext.Current.CancellationToken);

        Grpc.TruncateCollectionRequest request =
            Assert.IsType<Grpc.TruncateCollectionRequest>(server.Service.Requests["TruncateCollection"]);
        Assert.Equal("truncate_coll", request.CollectionName);
    }
}
