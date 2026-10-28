using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class SessionApiTests
{
    [Fact]
    public void Session_exposes_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        MilvusClientV2Session session = client.Session("cluster-a");

        Assert.Equal("cluster-a", session.ClusterId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Session_rejects_null_or_blank_cluster_id(string? clusterId)
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // Verify.NotNullOrWhiteSpace throws ArgumentNullException for null and ArgumentException for
        // whitespace; ThrowsAny accepts both.
        Assert.ThrowsAny<ArgumentException>(() => client.Session(clusterId!));
    }

    [Fact]
    public async Task Session_search_routes_with_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();
        MilvusClientV2Session session = client.Session("cluster-a");

        await session.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 2
            },
            TestContext.Current.CancellationToken);

        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
    }

    [Fact]
    public async Task Session_query_routes_with_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();
        MilvusClientV2Session session = client.Session("cluster-a");

        await session.QueryAsync(
            new QueryReq { CollectionName = "coll", Expression = "id > 0" },
            TestContext.Current.CancellationToken);

        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
    }

    [Fact]
    public async Task Session_get_routes_with_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildSchema();

        MilvusClientV2Session session = client.Session("cluster-a");

        await session.GetAsync(
            new GetReq { CollectionName = "coll", Ids = new object[] { 1L, 2L } },
            TestContext.Current.CancellationToken);

        // Get travels on the Query wire, so the cluster id lands in the query params.
        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
        Assert.Equal("id in [1, 2]", server.Service.LastQueryExpression);
    }

    [Fact]
    public async Task Session_hybrid_search_routes_with_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();
        MilvusClientV2Session session = client.Session("cluster-a");

        await session.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                        MetricType = MetricType.L2,
                        Limit = 10
                    }
                ],
                Limit = 3
            },
            TestContext.Current.CancellationToken);

        var grpcRequest =
            Assert.IsType<Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);
        var byKey = grpcRequest.RankParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
    }

    [Fact]
    public async Task Session_query_iterator_routes_with_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        MilvusClientV2Session session = client.Session("cluster-a");

        int batches = 0;
        await foreach (IReadOnlyList<FieldData> batch in session
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = new QueryParameters { Limit = 1 }
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            batches++;
            Assert.NotNull(batch);
        }

        Assert.Equal(1, batches);

        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
    }

    [Fact]
    public async Task Session_search_iterator_routes_with_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.SearchIteratorToken = "tok";

        MilvusClientV2Session session = client.Session("cluster-a");

        await foreach (SingleResult page in session
            .SearchIteratorAsync(new SearchIteratorReq
            {
                CollectionName = "iter_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                BatchSize = 10
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.NotNull(page);
        }

        var grpcRequest = Assert.IsType<Grpc.SearchRequest>(server.Service.Requests["Search"]);
        var byKey = grpcRequest.SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
    }

    [Fact]
    public async Task Session_query_iterator_routes_cluster_id_on_seek_and_pages()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 5;

        MilvusClientV2Session session = client.Session("cluster-a");

        // A non-zero Offset drives the seek phase (plain, non-iterator queries) before the cursor loop.
        await foreach (IReadOnlyList<FieldData> batch in session
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = new QueryParameters { Limit = 5, Offset = 3 }
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.NotNull(batch);
        }

        // Every request -- the seek query plus every paged query -- carries the session's cluster id.
        Assert.True(server.Service.QueryRequests.Count >= 2);
        foreach (Grpc.QueryRequest request in server.Service.QueryRequests)
        {
            var byKey = request.QueryParams.ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal("cluster-a", byKey["cluster_id"]);
        }
    }

    [Fact]
    public async Task Session_search_iterator_routes_cluster_id_on_every_page()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.SearchIteratorToken = "tok";

        MilvusClientV2Session session = client.Session("cluster-a");

        int pages = 0;
        await foreach (SingleResult page in session
            .SearchIteratorAsync(new SearchIteratorReq
            {
                CollectionName = "iter_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 5,
                BatchSize = 2
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            pages++;
            Assert.NotNull(page);
        }

        // Limit 5 at BatchSize 2 yields three pages (2 + 2 + 1); every page's request carries the cluster id.
        Assert.Equal(3, pages);
        Assert.Equal(3, server.Service.SearchRequests.Count);
        foreach (Grpc.SearchRequest request in server.Service.SearchRequests)
        {
            var byKey = request.SearchParams.ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal("cluster-a", byKey["cluster_id"]);
        }
    }

    [Fact]
    public async Task Session_hybrid_search_drops_sub_request_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var subRequest = new AnnSearchReq
        {
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
            MetricType = MetricType.L2,
            Limit = 10
        };
        subRequest.ExtraParameters["cluster_id"] = "user-leg";

        MilvusClientV2Session session = client.Session("cluster-a");

        await session.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests = [subRequest],
                Limit = 3
            },
            TestContext.Current.CancellationToken);

        var grpcRequest =
            Assert.IsType<Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);

        // The session's cluster id rides the hybrid rank params.
        var rankByKey = grpcRequest.RankParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", rankByKey["cluster_id"]);

        // A caller-supplied cluster_id on a sub-request is dropped (flat search param and params JSON), so it
        // cannot bypass the session binding.
        Grpc.SearchRequest sub = Assert.Single(grpcRequest.Requests);
        var subParams = sub.SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.DoesNotContain("cluster_id", subParams.Keys);
        Assert.DoesNotContain("cluster_id", subParams["params"]);
    }

    [Fact]
    public async Task Session_cluster_id_overrides_caller_supplied_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.ExtraParameters["cluster_id"] = "user-cluster";

        MilvusClientV2Session session = client.Session("cluster-a");

        await session.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        // The session's cluster id wins, and the caller-supplied one is not duplicated (ToDictionary throws on
        // duplicate keys, so exactly one cluster_id entry reaches the wire).
        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
        // The cluster id is never embedded in the params JSON bag (matching the C++ SDK's erase).
        Assert.DoesNotContain("cluster_id", byKey["params"]);
    }

    [Fact]
    public async Task Search_plain_client_forwards_caller_supplied_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.ExtraParameters["cluster_id"] = "manual-cluster";

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        // Outside a session, manual cluster_id routing via ExtraParameters stays supported: the value reaches
        // the wire both as a flat search param and inside the params JSON.
        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("manual-cluster", byKey["cluster_id"]);
        Assert.Contains("cluster_id", byKey["params"]);
    }

    [Fact]
    public async Task Session_query_cluster_id_overrides_caller_supplied_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new QueryParameters();
        parameters.ExtraParameters["cluster_id"] = "user-query";

        MilvusClientV2Session session = client.Session("cluster-a");

        await session.QueryAsync(
            new QueryReq { CollectionName = "coll", Expression = "id > 0", Parameters = parameters },
            TestContext.Current.CancellationToken);

        // The session's cluster id wins on the Query wire and appears exactly once in QueryParams.
        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
        Assert.Single(grpcRequest.QueryParams, p => p.Key == "cluster_id");
    }

    [Fact]
    public async Task Session_hybrid_search_cluster_id_overrides_caller_supplied_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.ExtraParameters["cluster_id"] = "user-hybrid";

        MilvusClientV2Session session = client.Session("cluster-a");

        await session.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                        MetricType = MetricType.L2,
                        Limit = 10
                    }
                ],
                Limit = 3,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        // The session's cluster id wins on the hybrid rank params and appears exactly once.
        var grpcRequest =
            Assert.IsType<Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);
        var byKey = grpcRequest.RankParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
        Assert.Single(grpcRequest.RankParams, p => p.Key == "cluster_id");
    }

    [Fact]
    public async Task Session_query_iterator_cluster_id_overrides_caller_supplied_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        var parameters = new QueryParameters { Limit = 1 };
        parameters.ExtraParameters["cluster_id"] = "user-iter";

        MilvusClientV2Session session = client.Session("cluster-a");

        await foreach (IReadOnlyList<FieldData> batch in session
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = parameters
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.NotNull(batch);
        }

        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
        Assert.Single(grpcRequest.QueryParams, p => p.Key == "cluster_id");
    }

    [Fact]
    public async Task Session_search_iterator_cluster_id_overrides_caller_supplied_cluster_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.SearchIteratorToken = "tok";

        var parameters = new SearchParameters();
        parameters.ExtraParameters["cluster_id"] = "user-iter";

        MilvusClientV2Session session = client.Session("cluster-a");

        await foreach (SingleResult page in session
            .SearchIteratorAsync(new SearchIteratorReq
            {
                CollectionName = "iter_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                BatchSize = 10,
                Parameters = parameters
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.NotNull(page);
        }

        var grpcRequest = Assert.IsType<Grpc.SearchRequest>(server.Service.Requests["Search"]);
        var byKey = grpcRequest.SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
        Assert.Single(grpcRequest.SearchParams, p => p.Key == "cluster_id");
    }

    [Fact]
    public async Task Session_close_does_not_stop_already_created_iterators()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        MilvusClientV2Session session = client.Session("cluster-a");

        // An iterator created before Close() keeps working: it holds the parent client and cluster id
        // directly, matching the Java/C++ SDKs.
        QueryIterator iterator = session.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1000,
            Parameters = new QueryParameters { Limit = 1 }
        });
        session.Close();

        int batches = 0;
        await foreach (IReadOnlyList<FieldData> batch in
            iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
            batches++;
            Assert.NotNull(batch);
        }

        Assert.Equal(1, batches);
        // The already-created iterator still routes to the session's cluster on the wire.
        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("cluster-a", byKey["cluster_id"]);
    }

    [Fact]
    public async Task Session_closed_fails_all_operations()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildSchema();

        MilvusClientV2Session session = client.Session("cluster-a");
        session.Close();

        await Assert.ThrowsAsync<MilvusException>(() => session.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = MetricType.L2,
                Limit = 1
            },
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<MilvusException>(() => session.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                        MetricType = MetricType.L2,
                        Limit = 1
                    }
                ],
                Limit = 1
            },
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<MilvusException>(() => session.QueryAsync(
            new QueryReq { CollectionName = "coll", Expression = "id > 0" },
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<MilvusException>(() => session.GetAsync(
            new GetReq { CollectionName = "coll", Ids = new object[] { 1L } },
            TestContext.Current.CancellationToken));
        Assert.Throws<MilvusException>(() => session.QueryIteratorAsync(
            new QueryIteratorReq { CollectionName = "coll", BatchSize = 10 }));
        Assert.Throws<MilvusException>(() => session.SearchIteratorAsync(
            new SearchIteratorReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = MetricType.L2,
                Limit = 1
            }));
    }

    [Fact]
    public async Task Session_close_does_not_disconnect_parent_client()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        MilvusClientV2Session session = client.Session("cluster-a");
        session.Close();

        // The parent client keeps working after the session is closed, and plain (session-less) calls carry
        // no cluster id.
        SearchResp response = await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 2
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastSearchedCollection);
        Assert.Equal(2, response.Ids.LongIds!.Count);
        Assert.DoesNotContain(server.Service.LastSearchParams, p => p.Key == "cluster_id");
    }

    private static CollectionSchema BuildSchema()
    {
        var schema = new CollectionSchema { Name = "coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        return schema;
    }

    private static CollectionSchema BuildIteratorSchema()
    {
        var schema = new CollectionSchema { Name = "iter_coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateFloatVector("embedding", 4));
        return schema;
    }
}
