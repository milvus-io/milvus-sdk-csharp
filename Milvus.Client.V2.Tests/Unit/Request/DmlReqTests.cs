using System.Text.Json;

using Xunit;

using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class DmlReqTests
{
    [Fact]
    public void Insert_maps_collection_partition_and_row_count()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            PartitionName = "p1",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1, 2, 3 }) }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
        Assert.Equal(3u, grpc.NumRows);
        Assert.Single(grpc.FieldsData);
    }

    [Fact]
    public void Insert_defaults_partition_to_empty_string()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1 }) }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Assert.Equal("", grpc.PartitionName);
    }

    [Fact]
    public void Insert_serializes_scalar_fields()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateVarChar("title", new[] { "a", "b" }),
                FieldData.Create("score", new[] { 0.5f, 1.5f })
            }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Assert.Equal(3, grpc.FieldsData.Count);

        Grpc.FieldData id = grpc.FieldsData[0];
        Assert.Equal("id", id.FieldName);
        Assert.Equal(Grpc.DataType.Int64, id.Type);
        Assert.False(id.IsDynamic);
        Assert.Equal(new[] { 1L, 2L }, id.Scalars.LongData.Data);

        Grpc.FieldData title = grpc.FieldsData[1];
        Assert.Equal(Grpc.DataType.VarChar, title.Type);
        Assert.Equal(new[] { "a", "b" }, title.Scalars.StringData.Data);

        Grpc.FieldData score = grpc.FieldsData[2];
        Assert.Equal(Grpc.DataType.Float, score.Type);
        Assert.Equal(new[] { 0.5f, 1.5f }, score.Scalars.FloatData.Data);
    }

    [Fact]
    public void Insert_serializes_float_vector_field()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new[]
            {
                FieldData.CreateFloatVector("embedding", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f }),
                    new ReadOnlyMemory<float>(new[] { 0.4f, 0.5f, 0.6f })
                })
            }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Grpc.FieldData embedding = grpc.FieldsData[0];
        Assert.Equal("embedding", embedding.FieldName);
        Assert.Equal(Grpc.DataType.FloatVector, embedding.Type);
        Assert.False(embedding.IsDynamic);
        Assert.Equal(3, embedding.Vectors.Dim);
        Assert.Equal(new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f }, embedding.Vectors.FloatVector.Data);
    }

    [Fact]
    public void Insert_serializes_json_field()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new[] { FieldData.CreateJson("meta", new[] { "{\"a\":1}", "{\"a\":2}" }) }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Grpc.FieldData meta = grpc.FieldsData[0];
        Assert.Equal(Grpc.DataType.Json, meta.Type);
        Assert.False(meta.IsDynamic);
        Assert.Equal(2, meta.Scalars.JsonData.Data.Count);
        Assert.Equal("{\"a\":1}", meta.Scalars.JsonData.Data[0].ToStringUtf8());
        Assert.Equal("{\"a\":2}", meta.Scalars.JsonData.Data[1].ToStringUtf8());
    }

    [Fact]
    public void Insert_serializes_nullable_scalar_with_valid_data()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new[] { FieldData.Create<int?>("score", new int?[] { 1, null, 3 }) }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Grpc.FieldData score = grpc.FieldsData[0];
        Assert.Equal(Grpc.DataType.Int32, score.Type);
        Assert.Equal(new[] { 1, 3 }, score.Scalars.IntData.Data);
        Assert.Equal(new[] { true, false, true }, score.ValidData);
    }

    [Fact]
    public void Insert_aggregates_dynamic_fields_into_meta_json()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.Create("x", new long[] { 10, 20 }, isDynamic: true),
                FieldData.Create("y", new[] { "foo", "bar" }, isDynamic: true)
            }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Assert.Equal(2u, grpc.NumRows);
        Assert.Equal(2, grpc.FieldsData.Count);

        Grpc.FieldData meta = grpc.FieldsData[1];
        Assert.True(meta.IsDynamic);
        Assert.Equal("", meta.FieldName);
        Assert.Equal(Grpc.DataType.Json, meta.Type);

        Assert.Equal(2, meta.Scalars.JsonData.Data.Count);
        Assert.Equal("{\"x\":10,\"y\":\"foo\"}", meta.Scalars.JsonData.Data[0].ToStringUtf8());
        Assert.Equal("{\"x\":20,\"y\":\"bar\"}", meta.Scalars.JsonData.Data[1].ToStringUtf8());

        using JsonDocument row0 = JsonDocument.Parse(meta.Scalars.JsonData.Data[0].ToStringUtf8());
        Assert.Equal(10, row0.RootElement.GetProperty("x").GetInt64());
        Assert.Equal("foo", row0.RootElement.GetProperty("y").GetString());

        using JsonDocument row1 = JsonDocument.Parse(meta.Scalars.JsonData.Data[1].ToStringUtf8());
        Assert.Equal(20, row1.RootElement.GetProperty("x").GetInt64());
        Assert.Equal("bar", row1.RootElement.GetProperty("y").GetString());
    }

    [Fact]
    public void Insert_aggregates_only_dynamic_fields_into_single_meta_json()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new[] { FieldData.Create("x", new long[] { 5 }, isDynamic: true) }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Assert.Single(grpc.FieldsData);
        Grpc.FieldData meta = grpc.FieldsData[0];
        Assert.True(meta.IsDynamic);
        Assert.Equal("", meta.FieldName);
        Assert.Equal("{\"x\":5}", meta.Scalars.JsonData.Data[0].ToStringUtf8());
    }

    [Fact]
    public void Insert_throws_when_collection_name_blank()
    {
        var request = new InsertReq
        {
            CollectionName = " ",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1 }) }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcInsertRequest());
    }

    [Fact]
    public void Insert_throws_when_data_empty()
    {
        var request = new InsertReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcInsertRequest());
    }

    [Fact]
    public void Upsert_maps_collection_partition_and_row_count()
    {
        var request = new UpsertReq
        {
            CollectionName = "book",
            PartitionName = "p1",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1, 2 }) }
        };

        Grpc.UpsertRequest grpc = request.ToGrpcUpsertRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
        Assert.Equal(2u, grpc.NumRows);
        Assert.Single(grpc.FieldsData);

        request.PartitionName = null;
        grpc = request.ToGrpcUpsertRequest();
        Assert.Equal("", grpc.PartitionName);
    }

    [Fact]
    public void Upsert_maps_partial_update_flag()
    {
        var request = new UpsertReq
        {
            CollectionName = "book",
            PartialUpdate = true,
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1 }) }
        };

        Grpc.UpsertRequest grpc = request.ToGrpcUpsertRequest();

        Assert.True(grpc.PartialUpdate);
    }

    [Fact]
    public void Upsert_maps_field_ops_and_implies_partial_update()
    {
        var request = new UpsertReq
        {
            CollectionName = "book",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1 }) },
            FieldOps = new[]
            {
                new FieldPartialUpdateOp("tags", FieldPartialUpdateOpType.ArrayAppend),
                new FieldPartialUpdateOp("score", FieldPartialUpdateOpType.Replace)
            }
        };

        Grpc.UpsertRequest grpc = request.ToGrpcUpsertRequest();

        Assert.True(grpc.PartialUpdate);
        Assert.Equal(2, grpc.FieldOps.Count);
        Assert.Equal("tags", grpc.FieldOps[0].FieldName);
        Assert.Equal(Grpc.FieldPartialUpdateOp.Types.OpType.ArrayAppend, grpc.FieldOps[0].Op);
        Assert.Equal("score", grpc.FieldOps[1].FieldName);
        Assert.Equal(Grpc.FieldPartialUpdateOp.Types.OpType.Replace, grpc.FieldOps[1].Op);
    }

    [Fact]
    public void Upsert_replace_only_field_ops_do_not_imply_partial_update()
    {
        var request = new UpsertReq
        {
            CollectionName = "book",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1 }) },
            FieldOps = new[]
            {
                new FieldPartialUpdateOp("tags", FieldPartialUpdateOpType.Replace)
            }
        };

        Grpc.UpsertRequest grpc = request.ToGrpcUpsertRequest();

        // Only a non-REPLACE op implies PartialUpdate (matching Java's isPartialUpdate); a REPLACE-only list
        // with PartialUpdate=false stays a full-replace upsert.
        Assert.False(grpc.PartialUpdate);
        Assert.Single(grpc.FieldOps);
    }

    [Fact]
    public void Upsert_serializes_scalar_and_vector_fields()
    {
        var request = new UpsertReq
        {
            CollectionName = "book",
            ColumnsData = new FieldData[]
            {
                FieldData.Create("id", new long[] { 1 }),
                FieldData.CreateFloatVector("embedding", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f })
                })
            }
        };

        Grpc.UpsertRequest grpc = request.ToGrpcUpsertRequest();

        Assert.Equal(2, grpc.FieldsData.Count);
        Assert.Equal(new[] { 1L }, grpc.FieldsData[0].Scalars.LongData.Data);
        Assert.Equal(2, grpc.FieldsData[1].Vectors.Dim);
        Assert.Equal(new[] { 0.1f, 0.2f }, grpc.FieldsData[1].Vectors.FloatVector.Data);
    }

    [Fact]
    public void Upsert_aggregates_dynamic_fields_into_meta_json()
    {
        var request = new UpsertReq
        {
            CollectionName = "book",
            ColumnsData = new FieldData[]
            {
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.Create("x", new long[] { 10, 20 }, isDynamic: true)
            }
        };

        Grpc.UpsertRequest grpc = request.ToGrpcUpsertRequest();

        Assert.Equal(2, grpc.FieldsData.Count);

        Grpc.FieldData meta = grpc.FieldsData[1];
        Assert.True(meta.IsDynamic);
        Assert.Equal("", meta.FieldName);
        Assert.Equal(Grpc.DataType.Json, meta.Type);
        Assert.Equal("{\"x\":10}", meta.Scalars.JsonData.Data[0].ToStringUtf8());
        Assert.Equal("{\"x\":20}", meta.Scalars.JsonData.Data[1].ToStringUtf8());

        using JsonDocument row0 = JsonDocument.Parse(meta.Scalars.JsonData.Data[0].ToStringUtf8());
        Assert.Equal(10, row0.RootElement.GetProperty("x").GetInt64());
    }

    [Fact]
    public void Insert_omits_null_dynamic_field_values_from_meta_json()
    {
        var request = new InsertReq
        {
            CollectionName = "book",
            ColumnsData = new FieldData[]
            {
                FieldData.Create("id", new long[] { 1 }),
                FieldData.Create("x", new long?[] { null }, isDynamic: true)
            }
        };

        Grpc.InsertRequest grpc = request.ToGrpcInsertRequest();

        Grpc.FieldData meta = grpc.FieldsData[^1];
        Assert.True(meta.IsDynamic);
        // A null dynamic value is omitted entirely (not written as an explicit JSON null), matching V1.
        Assert.Equal("{}", meta.Scalars.JsonData.Data[0].ToStringUtf8());
    }

    [Fact]
    public void Upsert_throws_when_collection_name_blank()
    {
        var request = new UpsertReq
        {
            CollectionName = " ",
            ColumnsData = new[] { FieldData.Create("id", new long[] { 1 }) }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcUpsertRequest());
    }

    [Fact]
    public void Upsert_throws_when_data_empty()
    {
        var request = new UpsertReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcUpsertRequest());
    }

    [Fact]
    public void Delete_maps_collection_partition_and_expression()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            PartitionName = "p1",
            Expression = "id in [1, 2, 3]"
        };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
        Assert.Equal("id in [1, 2, 3]", grpc.Expr);
    }

    [Fact]
    public void Delete_rejects_mixed_and_null_id_elements()
    {
        var mixed = new DeleteReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L, "two" }
        };
        Assert.Throws<ArgumentException>(() => mixed.ToGrpcDeleteRequest("id"));

        var withNull = new DeleteReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L, null!, 3L }
        };
        Assert.Throws<ArgumentException>(() => withNull.ToGrpcDeleteRequest("id"));
    }

    [Fact]
    public void Delete_maps_consistency_level()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Expression = "id == 1",
            ConsistencyLevel = ConsistencyLevel.Session
        };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest();

        Assert.Equal(Grpc.ConsistencyLevel.Session, grpc.ConsistencyLevel);
    }

    [Fact]
    public void Delete_leaves_consistency_level_unset_when_null()
    {
        var request = new DeleteReq { CollectionName = "book", Expression = "id == 1" };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest();

        // Unset maps to the default enum value (Strong/0), the proto default for consistency_level.
        Assert.Equal(default, grpc.ConsistencyLevel);
    }

    [Fact]
    public void Delete_defaults_partition_to_empty_string()
    {
        var request = new DeleteReq { CollectionName = "book", Expression = "id == 1" };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest();

        Assert.Equal("", grpc.PartitionName);
        Assert.Equal("id == 1", grpc.Expr);
    }

    [Fact]
    public void Delete_throws_when_collection_name_blank()
    {
        var request = new DeleteReq { CollectionName = " ", Expression = "id == 1" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDeleteRequest());
    }

    [Fact]
    public void Delete_throws_when_expression_blank()
    {
        var request = new DeleteReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDeleteRequest());
    }

    [Fact]
    public void Delete_builds_expression_from_ids_using_primary_key_field()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Ids = new object[] { 1, 2, 3 }
        };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest("id");

        Assert.Equal("id in [1, 2, 3]", grpc.Expr);
        Assert.Equal("", grpc.PartitionName);
    }

    [Fact]
    public void Delete_builds_ids_expression_repeatedly_without_mutating_expression()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Ids = new object[] { 1, 2 }
        };

        Grpc.DeleteRequest first = request.ToGrpcDeleteRequest("id");
        Assert.Equal("id in [1, 2]", first.Expr);

        // A second conversion must not trip the Expression-and-Ids mutual exclusion (the generated expression
        // is local, not written back to the request), and request.Expression stays untouched.
        Grpc.DeleteRequest second = request.ToGrpcDeleteRequest("id");
        Assert.Equal("id in [1, 2]", second.Expr);
        Assert.Equal("", request.Expression);
    }

    [Fact]
    public void Delete_quotes_and_escapes_string_ids()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Ids = new object[] { "a\"b", "c\\d" }
        };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest("id");

        Assert.Equal("id in [\"a\\\"b\", \"c\\\\d\"]", grpc.Expr);
    }

    [Fact]
    public void Delete_throws_when_expression_and_ids_both_set()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Expression = "id == 1",
            Ids = new object[] { 1 }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcDeleteRequest("id"));
    }

    [Fact]
    public void Delete_throws_when_ids_set_without_primary_key_field()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Ids = new object[] { 1 }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcDeleteRequest());
    }

    [Fact]
    public void Delete_maps_filter_templates_to_expr_template_values()
    {
        var request = new DeleteReq
        {
            CollectionName = "book",
            Expression = "id in {ids}",
        };
        request.FilterTemplates["ids"] = new[] { 1, 2, 3 };

        Grpc.DeleteRequest grpc = request.ToGrpcDeleteRequest();

        Assert.Equal("id in {ids}", grpc.Expr);
        Assert.True(grpc.ExprTemplateValues.ContainsKey("ids"));
    }

    [Fact]
    public void Get_maps_ids_to_in_expression()
    {
        var request = new GetReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L, 2L, 3L }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("id in [1, 2, 3]", grpc.Expr);
        Assert.Empty(grpc.OutputFields);
    }

    [Fact]
    public void Get_escapes_string_primary_keys()
    {
        var request = new GetReq
        {
            CollectionName = "book",
            Ids = new object[] { "a\"b", "c\\d", "plain" }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("id in [\"a\\\"b\", \"c\\\\d\", \"plain\"]", grpc.Expr);
    }

    [Fact]
    public void Get_supports_mixed_id_types()
    {
        var request = new GetReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L, "two" }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal("id in [1, \"two\"]", grpc.Expr);
    }

    [Fact]
    public void Get_maps_output_fields()
    {
        var request = new GetReq
        {
            CollectionName = "book",
            Ids = new object[] { 1L },
            OutputFields = new[] { "title", "embedding" }
        };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");

        Assert.Equal(new[] { "title", "embedding" }, grpc.OutputFields);
    }

    [Fact]
    public void Get_omits_output_fields_when_null_or_empty()
    {
        var request = new GetReq { CollectionName = "book", Ids = new object[] { 1L } };

        Grpc.QueryRequest grpc = request.ToGrpcQueryRequest("id");
        Assert.Empty(grpc.OutputFields);

        request.OutputFields = Array.Empty<string>();
        grpc = request.ToGrpcQueryRequest("id");
        Assert.Empty(grpc.OutputFields);
    }

    [Fact]
    public void Get_throws_when_collection_name_blank()
    {
        var request = new GetReq { Ids = new object[] { 1L } };
        Assert.Throws<ArgumentException>(() => request.ToGrpcQueryRequest("id"));
    }

    [Fact]
    public void Get_throws_when_ids_empty()
    {
        var request = new GetReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcQueryRequest("id"));
    }
}
