using Xunit;

using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class DqlReqTests
{
    [Fact]
    public void QueryIterator_validates_offset_is_rejected()
    {
        var request = new QueryIteratorReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Offset = 10 }
        };

        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void QueryIterator_accepts_defaults()
    {
        var request = new QueryIteratorReq { CollectionName = "book" };
        request.Validate(); // should not throw
        Assert.Equal(1000, request.BatchSize);
    }

    [Fact]
    public void QueryIterator_rejects_batch_size_out_of_range()
    {
        var request = new QueryIteratorReq { CollectionName = "book", BatchSize = 20000 };
        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
    }

    [Fact]
    public void SearchIterator_rejects_offset_and_multi_vector()
    {
        var multi = new SearchIteratorReq
        {
            CollectionName = "book",
            VectorFieldName = "embedding",
            Vectors = new[]
            {
                new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }),
                new ReadOnlyMemory<float>(new[] { 0.3f, 0.4f })
            },
            MetricType = SimilarityMetricType.L2
        };
        Assert.Throws<ArgumentException>(() => multi.Validate());

        var offset = new SearchIteratorReq
        {
            CollectionName = "book",
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }) },
            MetricType = SimilarityMetricType.L2,
            Parameters = new SearchParameters { Offset = 5 }
        };
        Assert.Throws<ArgumentException>(() => offset.Validate());
    }

    [Fact]
    public void SearchIterator_requires_exactly_one_vector_input()
    {
        var request = new SearchIteratorReq
        {
            CollectionName = "book",
            VectorFieldName = "embedding",
            MetricType = SimilarityMetricType.L2
        };
        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void HybridSearch_validates_empty_requests()
    {
        var request = new HybridSearchReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void HybridSearch_requires_weighted_reranker_match()
    {
        var request = new HybridSearchReq
        {
            CollectionName = "book",
            SearchRequests = new[]
            {
                new SearchReq
                {
                    CollectionName = "book",
                    VectorFieldName = "embedding",
                    Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }) },
                    MetricType = SimilarityMetricType.L2,
                    Limit = 10
                }
            },
            Limit = 10,
            Reranker = new WeightedReranker(0.5f, 0.5f)
        };

        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void QueryReq_unset_consistency_uses_collection_default()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            Parameters = new QueryParameters() // ConsistencyLevel left unset
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Assert.True(grpc.UseDefaultConsistency); // collection default, not forced Session

        request.Parameters.ConsistencyLevel = ConsistencyLevel.Strong;
        grpc = request.ToGrpcQueryRequest();
        Assert.False(grpc.UseDefaultConsistency);
        Assert.Equal(Grpc.ConsistencyLevel.Strong, grpc.ConsistencyLevel);
    }
}
