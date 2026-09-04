using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class DqlTests
{
    [Fact]
    public async Task Search_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SearchResp response = await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 2
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastSearchedCollection);
        Assert.Equal(2, server.Service.LastSearchTopK);
        Assert.Equal(2, response.Ids.LongIds!.Count);
        Assert.Equal(2, response.Scores.Count);
        Assert.Equal(0.5f, response.Scores[0]);
    }

    [Fact]
    public async Task Search_uses_ts_cache_for_session_consistency()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();

        // Simulate a prior DML so the ts cache is populated.
        CollectionTsCache.Instance.Clear();
        CollectionTsCache.Instance.Set(server.Uri, "default", "session_coll", 100);

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "session_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = new SearchParameters { ConsistencyLevel = ConsistencyLevel.Session }
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(100UL, server.Service.LastSearchGuaranteeTimestamp);
    }

    [Fact]
    public async Task Search_serializes_ignore_growing_and_graceful_time()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters { GracefulTime = 5000 };
        parameters.SetIgnoreGrowing(true);

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("true", byKey["ignore_growing"]);
        Assert.Equal("5000", byKey["graceful_time"]);
    }

    [Fact]
    public async Task Query_forwards_expression()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        QueryResp response = await client.QueryAsync(
            new QueryReq
            {
                CollectionName = "coll",
                Expression = "id in [1, 2]",
                Parameters = new QueryParameters { OutputFields = { "id" } }
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastQueriedCollection);
        Assert.Equal("id in [1, 2]", server.Service.LastQueryExpression);
        Assert.Single(response.FieldsData);
    }

    [Fact]
    public async Task Get_builds_query_by_primary_key()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // The mock DescribeCollection has no schema, so Get needs one via the (mock) describe path.
        // Give the mock a describable schema by adding a primary-key field through DescribeCollectionResp.
        server.Service.DescribeSchema = BuildSchema();

        GetResp response = await client.GetAsync(
            new GetReq { CollectionName = "coll", Ids = new object[] { 1L, 2L } },
            TestContext.Current.CancellationToken);

        Assert.Single(response.FieldsData);
        Assert.Equal("id in [1, 2]", server.Service.LastQueryExpression);
    }

    private static CollectionSchema BuildSchema()
    {
        var schema = new CollectionSchema { Name = "coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        return schema;
    }
}
