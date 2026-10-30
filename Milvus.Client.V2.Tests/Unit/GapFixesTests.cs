using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit;

[Trait("Category", "Unit")]
public class GapFixesTests
{
    [Theory]
    [InlineData(AggregationDirection.Asc, "f:asc")]
    [InlineData(AggregationDirection.Desc, "f:desc")]
    public void OrderByField_serializes_field_direction(AggregationDirection direction, string expected)
        => Assert.Equal(expected, new OrderByField("f", direction).ToWireString());

    [Fact]
    public void OrderByField_rejects_empty_field_name()
        => Assert.Throws<ArgumentException>(() => new OrderByField("  "));

    [Fact]
    public void QueryIteratorCursor_rejects_both_pk_types()
        => Assert.Throws<ArgumentException>(() => new QueryIteratorCursor(1UL, intPk: 2L, strPk: "x"));

    [Fact]
    public void QueryIteratorCursor_allows_keyless_cursor()
    {
        // A keyless cursor (no pk) is valid: it resumes from the beginning of the collection on the pinned
        // snapshot.
        var cursor = new QueryIteratorCursor(42UL);
        Assert.Equal(42UL, cursor.SessionTs);
        Assert.Null(cursor.IntPk);
        Assert.Null(cursor.StrPk);
    }

    [Fact]
    public void QueryIteratorCursor_rejects_negative_element_offset()
        => Assert.Throws<ArgumentException>(() => new QueryIteratorCursor(1UL, intPk: 2L, lastElementOffset: -1));

    [Fact]
    public void QueryIteratorCursor_rejects_element_offset_without_pk()
        => Assert.Throws<ArgumentException>(() => new QueryIteratorCursor(1UL, lastElementOffset: 3));

    [Fact]
    public void QueryIteratorCursor_allows_zero_session_ts()
    {
        // A zero sessionTs is valid: it means no snapshot is pinned, and the server picks its own per page.
        var cursor = new QueryIteratorCursor(0UL, intPk: 1L);
        Assert.Equal(0UL, cursor.SessionTs);
    }

    [Fact]
    public void QueryReq_forwards_typed_order_and_group_by_fields()
    {
        var request = new QueryReq
        {
            CollectionName = "book",
            Expression = "id > 0",
            Parameters = new QueryParameters()
        };
        request.Parameters.OrderByFields.Add(new OrderByField("title", AggregationDirection.Desc));
        request.Parameters.OrderByFields.Add(new OrderByField("pages"));
        request.Parameters.GroupByFields.Add("author");
        request.Parameters.GroupByFields.Add("year");

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        var byKey = grpc.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("title:desc,pages:asc", byKey["order_by_fields"]);
        Assert.Equal("author,year", byKey["group_by_fields"]);
    }

    [Fact]
    public void CreateCollectionReq_forwards_external_field()
    {
        var field = new FieldSchema("f", DataType.VarChar, isPrimaryKey: true) { ExternalField = "ext_1" };

        Grpc.FieldSchema grpc = CreateCollectionReq.ConvertToGrpcFieldSchema(field);

        Assert.Equal("ext_1", grpc.ExternalField);
    }

    [Fact]
    public void DescribeCollectionResp_reads_external_field()
    {
        var grpc = new Grpc.DescribeCollectionResponse
        {
            Schema = new Grpc.CollectionSchema
            {
                Name = "coll",
                Fields =
                {
                    new Grpc.FieldSchema { Name = "f", DataType = Grpc.DataType.VarChar, ExternalField = "ext_1" }
                }
            }
        };

        DescribeCollectionResp response = DescribeCollectionResp.FromGrpc(grpc);

        Assert.Equal("ext_1", response.Schema.Fields[0].ExternalField);
        Assert.Null(response.Schema.Fields[0].MaxLength);
    }

    [Fact]
    public void GetPersistentSegmentInfoReq_forwards_states_filter()
    {
        var request = new GetPersistentSegmentInfoReq
        {
            CollectionName = "coll",
            States = new[] { SegmentState.Sealed, SegmentState.Flushed }
        };

        Grpc.GetPersistentSegmentInfoRequest grpc = request.ToGrpcGetPersistentSegmentInfoRequest();

        Assert.Equal(new[] { Grpc.SegmentState.Sealed, Grpc.SegmentState.Flushed }, grpc.States);
    }

    [Fact]
    public void GetPersistentSegmentInfoReq_leaves_states_empty_when_unset()
    {
        var request = new GetPersistentSegmentInfoReq { CollectionName = "coll" };

        Grpc.GetPersistentSegmentInfoRequest grpc = request.ToGrpcGetPersistentSegmentInfoRequest();

        Assert.Empty(grpc.States);
    }
}
