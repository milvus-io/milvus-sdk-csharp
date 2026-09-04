using Xunit;

using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class DqlReqTests
{
    [Fact]
    public void QueryReq_forwards_request_level_database_name()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            DatabaseName = "other_db"
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("other_db", grpc.DbName);
    }

    [Fact]
    public void QueryReq_leaves_database_name_empty_when_unset()
    {
        var request = new QueryReq { CollectionName = "book", Expression = "id > 0" };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("", grpc.DbName);
    }

    [Fact]
    public void GetReq_forwards_request_level_database_name()
    {
        var request = new GetReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L },
            DatabaseName = "other_db"
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("other_db", grpc.DbName);
    }

    [Fact]
    public void QueryIterator_accepts_offset()
    {
        var request = new QueryIteratorReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Offset = 10 }
        };

        request.Validate(); // should not throw
    }

    [Fact]
    public void QueryIterator_rejects_negative_offset()
    {
        var request = new QueryIteratorReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Offset = -1 }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
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
    public void QueryIterator_rejects_negative_limit()
    {
        var request = new QueryIteratorReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Limit = -1 }
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
    }

    [Fact]
    public void QueryIterator_rejects_zero_limit()
    {
        var request = new QueryIteratorReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Limit = 0 }
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
    }

    [Fact]
    public void SearchIterator_rejects_negative_limit()
    {
        var request = new SearchIteratorReq
        {
            CollectionName = "book",
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = -1
        };
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
                new AnnSearchReq
                {
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
    public void AnnSearchReq_rejects_limit_plus_offset_beyond_server_bound()
    {
        var request = new AnnSearchReq
        {
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 16384,
            Offset = 1
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

    [Fact]
    public void QueryReq_maps_ignore_growing_timezone_and_filter_templates()
    {
        var parameters = new QueryParameters
        {
            IgnoreGrowing = true,
            Timezone = "+08:00"
        };
        parameters.FilterTemplates["status"] = "active";
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "status == @status",
            Parameters = parameters
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        var queryParams = grpc.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("true", queryParams["ignore_growing"]);
        Assert.Equal("+08:00", queryParams["timezone"]);
        Assert.True(grpc.ExprTemplateValues.ContainsKey("status"));
        Assert.Equal("active", grpc.ExprTemplateValues["status"].StringVal);
    }

    [Fact]
    public void QueryReq_maps_numeric_filter_template_array()
    {
        var parameters = new QueryParameters();
        parameters.FilterTemplates["ids"] = new long[] { 1, 2, 3 };
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id in {ids}",
            Parameters = parameters
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Grpc.TemplateValue template = grpc.ExprTemplateValues["ids"];
        Assert.Equal(new long[] { 1, 2, 3 }, template.ArrayVal.LongData.Data);
    }

    [Fact]
    public void QueryReq_accepts_short_first_element_filter_template_array()
    {
        var parameters = new QueryParameters();
        parameters.FilterTemplates["ids"] = new object[] { (short)1, 2 };
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id in {ids}",
            Parameters = parameters
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Grpc.TemplateValue template = grpc.ExprTemplateValues["ids"];
        Assert.Equal(new long[] { 1, 2 }, template.ArrayVal.LongData.Data);
    }

    [Theory]
    [InlineData((sbyte)1)]
    [InlineData((ushort)1)]
    [InlineData(1UL)]
    public void QueryReq_accepts_full_integral_filter_template_array(object first)
    {
        var parameters = new QueryParameters();
        parameters.FilterTemplates["ids"] = new object[] { first, Convert.ChangeType(first, first.GetType(), System.Globalization.CultureInfo.InvariantCulture)! };
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id in {ids}",
            Parameters = parameters
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Grpc.TemplateValue template = grpc.ExprTemplateValues["ids"];
        Assert.Equal(2, template.ArrayVal.LongData.Data.Count);
        Assert.Equal(Convert.ToInt64(first, System.Globalization.CultureInfo.InvariantCulture), template.ArrayVal.LongData.Data[0]);
    }

    [Fact]
    public void QueryReq_maps_byte_filter_template_array()
    {
        var parameters = new QueryParameters();
        parameters.FilterTemplates["ids"] = new byte[] { 1, 2, 3 };
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id in {ids}",
            Parameters = parameters
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Grpc.TemplateValue template = grpc.ExprTemplateValues["ids"];
        Assert.Equal(new long[] { 1, 2, 3 }, template.ArrayVal.LongData.Data);
    }

    [Fact]
    public void QueryReq_rejects_mixed_type_filter_template_array()
    {
        var parameters = new QueryParameters();
        parameters.FilterTemplates["ids"] = new object[] { 1L, "x" };
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id in {ids}",
            Parameters = parameters
        };

        // Mixed-type arrays must fail with a clear ArgumentException (matching C++), not a raw FormatException.
        Assert.Throws<ArgumentException>(() => request.ToGrpcQueryRequest());
    }

    [Fact]
    public void QueryReq_builds_expression_from_parameters_ids_using_primary_key_field()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters
            {
                Ids = new object[] { 1, 2, 3 }
            }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("id in [1, 2, 3]", grpc.Expr);
        Assert.True(grpc.UseDefaultConsistency);
    }

    [Fact]
    public void QueryReq_quotes_and_escapes_string_ids()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters
            {
                Ids = new object[] { "a\"b", "c\\d" }
            }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("id in [\"a\\\"b\", \"c\\\\d\"]", grpc.Expr);
    }

    [Fact]
    public void QueryReq_throws_when_expression_and_ids_both_set()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            Parameters = new QueryParameters { Ids = new object[] { 1 } }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcQueryRequest("id"));
    }

    [Fact]
    public void QueryReq_allows_fetch_all_when_expression_and_ids_unset()
    {
        // An empty expression with no ids means fetch-all, matching the C++/Java SDKs (the proxy treats an
        // empty expr as "all rows").
        var request = new QueryReq { CollectionName = "book" };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Assert.Equal("", grpc.Expr);
    }

    [Fact]
    public void QueryReq_normalizes_whitespace_only_expression_to_empty()
    {
        // A whitespace-only expression is treated as "no expression" (fetch-all); the wire must carry an empty
        // expr, not the literal whitespace that would fail the proxy's expression parser.
        var request = new QueryReq { CollectionName = "book", Expression = "   " };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Assert.Equal("", grpc.Expr);
    }

    [Fact]
    public void QueryReq_throws_when_ids_set_without_primary_key_field()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Ids = new object[] { 1 } }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcQueryRequest());
    }

    [Fact]
    public void QueryReq_rejects_null_id_elements()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Parameters = new QueryParameters { Ids = new object[] { 1L, null!, 3L } }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcQueryRequest("id"));
    }

    [Fact]
    public void GetReq_maps_partition_name()
    {
        var request = new GetReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L },
            PartitionName = "p1"
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal(new[] { "p1" }, grpc.PartitionNames);
    }

    [Fact]
    public void GetReq_omits_partition_names_when_not_set()
    {
        var request = new GetReq { CollectionName = "book", Ids = new object[] { 1L } };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Empty(grpc.PartitionNames);
    }

    [Fact]
    public void QueryReq_validates_limit_offset_range()
    {
        // Limit=0 (or negative) would make the server treat the query as unlimited.
        var zeroLimit = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            Parameters = new QueryParameters { Limit = 0 }
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => zeroLimit.ToGrpcQueryRequest());

        var tooLarge = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            Parameters = new QueryParameters { Limit = 16384, Offset = 1 }
        };
        Assert.Throws<ArgumentException>(() => tooLarge.ToGrpcQueryRequest());
    }

    [Fact]
    public void QueryReq_accepts_unset_limit_and_offset()
    {
        var request = new QueryReq { CollectionName = "book", Expression = "id > 0" };
        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();
        Assert.Empty(grpc.QueryParams);
    }

    [Fact]
    public void QueryReq_forwards_extra_parameters()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            Parameters = new QueryParameters
            {
                ExtraParameters = { ["advanced_query"] = "text_match(title, 'time')" }
            }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest();

        Assert.Contains(new Grpc.KeyValuePair { Key = "advanced_query", Value = "text_match(title, 'time')" },
            grpc.QueryParams);
    }

    [Fact]
    public void SearchReq_encodes_dense_float_placeholder_little_endian()
    {
        // 1.0f = 0x3F800000 in little-endian bytes (00 00 80 3F), independent of host byte order.
        var request = new SearchReq
        {
            CollectionName = "book",
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1.0f, 2.0f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 1
        };

        Grpc.PlaceholderValue placeholder = request.ToPlaceholderValue();

        Assert.Equal(Grpc.PlaceholderType.FloatVector, placeholder.Type);
        byte[] raw = placeholder.Values[0].ToByteArray();
        Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x40 }, raw);
    }

    [Fact]
    public void SearchReq_encodes_embedding_list_placeholder()
    {
        // An embedding list packs its vectors' floats into a single EmbListFloatVector placeholder value.
        var request = new SearchReq
        {
            CollectionName = "book",
            VectorFieldName = "clips[vector]",
            EmbeddingLists =
            [
                new EmbeddingList(new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1.0f, 2.0f }),
                    new ReadOnlyMemory<float>(new[] { 3.0f, 4.0f })
                })
            ],
            MetricType = SimilarityMetricType.MaxSimCosine,
            Limit = 1
        };

        Grpc.PlaceholderValue placeholder = request.ToPlaceholderValue();

        Assert.Equal(Grpc.PlaceholderType.EmbListFloatVector, placeholder.Type);
        Assert.Single(placeholder.Values);
        byte[] raw = placeholder.Values[0].ToByteArray();
        Assert.Equal(new byte[] { 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x80, 0x40 }, raw);
    }
}
