using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Types;
using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class IndexApiTests
{
    // ---- Index ----

    [Fact]
    public async Task CreateIndex_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateIndexAsync(
            new CreateIndexReq
            {
                CollectionName = "coll",
                Indexes =
                [
                    new IndexParam("embedding", "idx1", IndexType.Hnsw, SimilarityMetricType.L2)
                    {
                        ExtraParams = { { "nlist", "128" } }
                    }
                ]
            },
            TestContext.Current.CancellationToken);

        CreateIndexRequest request = Assert.IsType<CreateIndexRequest>(server.Service.Requests["CreateIndex"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("embedding", request.FieldName);
        Assert.Equal("idx1", request.IndexName);
        Assert.Contains(request.ExtraParams, p => p.Key == "index_type" && p.Value == "HNSW");
        Assert.Contains(request.ExtraParams, p => p.Key == "metric_type" && p.Value == "L2");
        Assert.Contains(request.ExtraParams, p => p.Key == "nlist" && p.Value == "128");
    }

    [Fact]
    public async Task CreateIndex_with_multiple_indexes_forwards_each()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateIndexAsync(
            new CreateIndexReq
            {
                CollectionName = "coll",
                Sync = false,
                Indexes =
                [
                    new IndexParam("embedding", "idx_a", IndexType.Hnsw, SimilarityMetricType.L2),
                    new IndexParam("text", "idx_b", IndexType.Bitmap, null)
                ]
            },
            TestContext.Current.CancellationToken);

        // Sync=false so no DescribeIndex poll follows; each index is forwarded as its own CreateIndex RPC.
        Assert.Equal(2, server.Service.CreateIndexRequests.Count);
        Assert.Equal("idx_a", server.Service.CreateIndexRequests[0].IndexName);
        Assert.Contains(server.Service.CreateIndexRequests[0].ExtraParams, p => p.Key == "index_type" && p.Value == "HNSW");
        Assert.Equal("idx_b", server.Service.CreateIndexRequests[1].IndexName);
        Assert.Contains(server.Service.CreateIndexRequests[1].ExtraParams, p => p.Key == "index_type" && p.Value == "BITMAP");
    }

    [Fact]
    public async Task CreateIndex_sync_waits_for_index_to_finish()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateIndexAsync(
            new CreateIndexReq { CollectionName = "coll", Indexes = [new IndexParam("embedding")] },
            TestContext.Current.CancellationToken);

        Assert.True(server.Service.Requests.ContainsKey("CreateIndex"));
        Assert.True(server.Service.Requests.ContainsKey("DescribeIndex"));
    }

    [Fact]
    public async Task CreateIndex_sync_false_skips_describe_index()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateIndexAsync(
            new CreateIndexReq { CollectionName = "coll", Indexes = [new IndexParam("embedding")], Sync = false },
            TestContext.Current.CancellationToken);

        Assert.True(server.Service.Requests.ContainsKey("CreateIndex"));
        Assert.False(server.Service.Requests.ContainsKey("DescribeIndex"));
    }

    [Fact]
    public async Task DropIndex_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropIndexAsync(
            new DropIndexReq { CollectionName = "coll", FieldName = "embedding" },
            TestContext.Current.CancellationToken);

        DropIndexRequest request = Assert.IsType<DropIndexRequest>(server.Service.Requests["DropIndex"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("embedding", request.FieldName);
        Assert.Equal("", request.IndexName);
    }

    [Fact]
    public async Task DescribeIndex_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DescribeIndexAsync(
            new DescribeIndexReq { CollectionName = "coll", FieldName = "embedding", IndexName = "idx1" },
            TestContext.Current.CancellationToken);

        DescribeIndexRequest request = Assert.IsType<DescribeIndexRequest>(server.Service.Requests["DescribeIndex"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("embedding", request.FieldName);
        Assert.Equal("idx1", request.IndexName);
    }

    [Fact]
    public async Task ListIndexes_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListIndexesAsync(
            new ListIndexesReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        DescribeIndexRequest request = Assert.IsType<DescribeIndexRequest>(server.Service.Requests["DescribeIndex"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("", request.FieldName);
        Assert.Equal("", request.IndexName);
    }

    [Fact]
    public async Task ListIndexes_returns_empty_when_no_index_exists()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeIndexNotFound = true;
        using MilvusClientV2 client = server.CreateClient();

        ListIndexesResp response = await client.ListIndexesAsync(
            new ListIndexesReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        Assert.Empty(response.Indexes);
    }

    [Fact]
    public async Task AlterIndexProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterIndexPropertiesAsync(
            new AlterIndexPropertiesReq
            {
                CollectionName = "coll",
                IndexName = "idx1",
                Properties = { { "nlist", "256" } }
            },
            TestContext.Current.CancellationToken);

        AlterIndexRequest request = Assert.IsType<AlterIndexRequest>(server.Service.Requests["AlterIndex"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("idx1", request.IndexName);
        Assert.Contains(request.ExtraParams, p => p.Key == "nlist" && p.Value == "256");
        Assert.Empty(request.DeleteKeys);
    }

    [Fact]
    public async Task DropIndexProperties_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropIndexPropertiesAsync(
            new DropIndexPropertiesReq
            {
                CollectionName = "coll",
                IndexName = "idx1",
                DeleteKeys = new[] { "key1", "key2" }
            },
            TestContext.Current.CancellationToken);

        AlterIndexRequest request = Assert.IsType<AlterIndexRequest>(server.Service.Requests["AlterIndex"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("idx1", request.IndexName);
        Assert.Equal(new[] { "key1", "key2" }, request.DeleteKeys);
        Assert.Empty(request.ExtraParams);
    }
}
