using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit;

[Trait("Category", "Unit")]
public class RowDataConverterTests
{
    private static CollectionSchema SimpleSchema(bool dynamic = false, bool autoId = true)
        => new()
        {
            EnableDynamicFields = dynamic,
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: autoId),
                new FieldSchema("name", DataType.VarChar) { MaxLength = 128 },
                new FieldSchema("score", DataType.Float),
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 }
            }
        };

    [Fact]
    public void ConvertRows_builds_columns_by_schema_type()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["name"] = "a", ["score"] = 1.5f, ["embedding"] = new[] { 1f, 2f } },
            new Dictionary<string, object?> { ["name"] = "b", ["score"] = 2.5, ["embedding"] = new[] { 3f, 4f } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, SimpleSchema(), isUpsert: false);

        // Auto-id pk omitted on insert; three schema fields provided.
        Assert.Equal(3, columns.Count);

        FieldData name = columns.First(c => c.FieldName == "name");
        Assert.Equal("a", name.GetValueAsObject(0));
        Assert.Equal("b", name.GetValueAsObject(1));

        FieldData score = columns.First(c => c.FieldName == "score");
        Assert.Equal(1.5f, score.GetValueAsObject(0));
        Assert.Equal(2.5f, score.GetValueAsObject(1));

        FieldData embedding = columns.First(c => c.FieldName == "embedding");
        var memory = (ReadOnlyMemory<float>)embedding.GetValueAsObject(0)!;
        Assert.Equal(new[] { 1f, 2f }, memory.ToArray());
    }

    [Fact]
    public void ConvertRows_rejects_unknown_field_without_dynamic_fields()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["name"] = "a", ["score"] = 1f, ["embedding"] = new[] { 1f, 2f }, ["oops"] = 1 }
        };

        Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(rows, SimpleSchema(), isUpsert: false));
    }

    [Fact]
    public void ConvertRows_aggregates_unknown_keys_into_dynamic_json()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["name"] = "a", ["score"] = 1f, ["embedding"] = new[] { 1f, 2f }, ["extra"] = "x", ["n"] = 3 },
            new Dictionary<string, object?> { ["name"] = "b", ["score"] = 2f, ["embedding"] = new[] { 3f, 4f }, ["extra"] = "y" }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, SimpleSchema(dynamic: true), isUpsert: false);

        FieldData dynamic = columns.Single(c => c.IsDynamic);
        Assert.Equal(2, dynamic.RowCount);
        Assert.Equal("{\"extra\":\"x\",\"n\":3}", dynamic.GetValueAsObject(0));
        Assert.Equal("{\"extra\":\"y\"}", dynamic.GetValueAsObject(1));
    }

    [Fact]
    public void ConvertRows_omits_auto_id_pk_and_rejects_missing_required_field()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["score"] = 1f, ["embedding"] = new[] { 1f, 2f } }
        };

        // name is required (not nullable, no default) -> throws.
        Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(rows, SimpleSchema(), isUpsert: false));

        var rowsWithName = new[]
        {
            new Dictionary<string, object?> { ["name"] = "a", ["score"] = 1f, ["embedding"] = new[] { 1f, 2f } }
        };
        List<FieldData> columns = RowDataConverter.ConvertRows(rowsWithName, SimpleSchema(), isUpsert: false);
        Assert.DoesNotContain(columns, c => c.FieldName == "id");
    }

    [Fact]
    public void ConvertRows_upsert_requires_primary_key()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["name"] = "a", ["score"] = 1f, ["embedding"] = new[] { 1f, 2f } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, SimpleSchema(), isUpsert: true);
        Assert.Equal(4, columns.Count);
        Assert.Equal(1L, columns.First(c => c.FieldName == "id").GetValueAsObject(0));
    }

    [Fact]
    public void ConvertRows_upsert_rejects_missing_auto_id_primary_key()
    {
        // An upsert must carry the primary key even when the schema marks it autoId; omitting it must throw.
        var rows = new[]
        {
            new Dictionary<string, object?> { ["name"] = "a", ["score"] = 1f, ["embedding"] = new[] { 1f, 2f } }
        };

        Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(rows, SimpleSchema(), isUpsert: true));
    }

    [Fact]
    public void ConvertRows_supports_nullable_null_values_with_valid_data()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                new FieldSchema("v", DataType.FloatVector) { Dimension = 2, Nullable = true },
                new FieldSchema("name", DataType.VarChar) { Nullable = true, MaxLength = 10 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["name"] = null, ["v"] = null },
            new Dictionary<string, object?> { ["name"] = "b", ["v"] = new[] { 1f, 2f } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData vec = columns.Single(c => c.FieldName == "v");
        Assert.NotNull(vec.ValidData);
        Assert.Equal(new[] { false, true }, vec.ValidData!.ToArray());

        // VarChar null rows are encoded via the scalar hasNull path (valid_data generated at encode time);
        // the exposed ValidData stays null but the row value reads back as null.
        FieldData name = columns.Single(c => c.FieldName == "name");
        Assert.Null(name.ValidData);
        Assert.Null(name.GetValueAsObject(0));
        Assert.Equal("b", name.GetValueAsObject(1));
    }

    [Fact]
    public void ConvertRows_fills_default_value_for_omitted_non_nullable_field()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true),
                new FieldSchema("name", DataType.VarChar) { MaxLength = 10, DefaultValue = "N/A" },
                new FieldSchema("score", DataType.Int32) { DefaultValue = 7 },
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["embedding"] = new[] { 1f, 2f } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData name = columns.Single(c => c.FieldName == "name");
        Assert.Equal("N/A", name.GetValueAsObject(0));

        FieldData score = columns.Single(c => c.FieldName == "score");
        Assert.Equal(7, score.GetValueAsObject(0));
    }

    [Fact]
    public void ConvertRows_partial_update_skips_omitted_fields()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("name", DataType.VarChar) { MaxLength = 10 },
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["name"] = "updated" }
        };

        // A partial upsert only carries the supplied fields; the vector field is skipped (the server leaves it
        // unchanged) rather than being required or defaulted.
        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: true, isPartialUpdate: true);

        Assert.Equal(2, columns.Count);
        Assert.Contains(columns, c => c.FieldName == "id");
        Assert.Contains(columns, c => c.FieldName == "name");
        Assert.DoesNotContain(columns, c => c.FieldName == "embedding");
    }

    [Fact]
    public void ConvertRows_full_upsert_still_requires_all_fields()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("name", DataType.VarChar) { MaxLength = 10 },
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["name"] = "updated" }
        };

        // Without PartialUpdate, omitting the vector field is an error.
        Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(rows, schema, isUpsert: true));
    }

    [Fact]
    public void ConvertRows_builds_sparse_float_vector_column()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("sparse", DataType.SparseFloatVector)
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["sparse"] = new SparseVector<float>(new[] { 0, 2 }, new[] { 0.1f, 0.2f }) },
            new Dictionary<string, object?> { ["id"] = 2L, ["sparse"] = new SparseVector<float>(new[] { 1, 3 }, new[] { 0.3f, 0.4f }) }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData sparse = columns.First(c => c.FieldName == "sparse");
        Assert.Equal(2, sparse.RowCount);
        var first = (SparseVector<float>)sparse.GetValueAsObject(0)!;
        Assert.Equal(new[] { 0, 2 }, first.Indices.ToArray());
    }

    [Fact]
    public void ConvertRows_builds_binary_vector_column()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("bin", DataType.BinaryVector) { Dimension = 16 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["bin"] = new byte[] { 0x0F, 0xF0 } },
            new Dictionary<string, object?> { ["id"] = 2L, ["bin"] = new byte[] { 0xFF, 0x00 } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData bin = columns.First(c => c.FieldName == "bin");
        Assert.Equal(2, bin.RowCount);
        Assert.Equal(new byte[] { 0x0F, 0xF0 }, ((ReadOnlyMemory<byte>)bin.GetValueAsObject(0)!).ToArray());
    }

    [Fact]
    public void ConvertRows_builds_fp16_and_bf16_columns()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("fp16", DataType.Float16Vector) { Dimension = 2 },
                new FieldSchema("bf16", DataType.BFloat16Vector) { Dimension = 2 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["fp16"] = new[] { Float16Utils.FloatToFp16(1f), (ushort)0 }, ["bf16"] = new[] { Float16Utils.FloatToBf16(1f), (ushort)0 } },
            new Dictionary<string, object?> { ["id"] = 2L, ["fp16"] = new[] { (ushort)0, Float16Utils.FloatToFp16(1f) }, ["bf16"] = new[] { (ushort)0, Float16Utils.FloatToBf16(1f) } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        Assert.Equal(2, columns.Count(c => c.FieldName is "fp16" or "bf16"));
    }

    [Fact]
    public void ConvertRows_builds_int8_vector_column()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("i8", DataType.Int8Vector) { Dimension = 4 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["i8"] = new sbyte[] { 1, 2, 3, 4 } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData i8 = columns.First(c => c.FieldName == "i8");
        Assert.Equal(new sbyte[] { 1, 2, 3, 4 }, ((ReadOnlyMemory<sbyte>)i8.GetValueAsObject(0)!).ToArray());
    }

    [Fact]
    public void ConvertRows_builds_json_column()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("meta", DataType.Json)
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["meta"] = "{\"a\":1}" }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData meta = columns.First(c => c.FieldName == "meta");
        Assert.Contains("a", meta.GetValueAsObject(0)!.ToString());
    }

    [Fact]
    public void ConvertRows_builds_struct_column()
    {
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("clips")
                {
                    MaxCapacity = 8,
                    Fields =
                    {
                        new FieldSchema("int32", DataType.Int32),
                        new FieldSchema("vector", DataType.FloatVector) { Dimension = 2 }
                    }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["clips"] = new[]
                {
                    new Dictionary<string, object?> { ["int32"] = 1, ["vector"] = new[] { 1f, 2f } },
                    new Dictionary<string, object?> { ["int32"] = 2, ["vector"] = new[] { 3f, 4f } }
                }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        StructFieldData clips = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "clips"));
        Assert.Equal(1, clips.RowCount);
        Assert.Equal(2, clips.Data[0]!.Count);
    }

    [Fact]
    public void ConvertRows_fills_default_for_mixed_provided_and_omitted_rows()
    {
        // Some rows provide a non-nullable defaulted field, others omit it: the omitted rows get the default
        // client-side (the "mixed" case the whole-column-absent test does not cover).
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                new FieldSchema("title", DataType.VarChar) { MaxLength = 10, DefaultValue = "N/A" },
                new FieldSchema("embedding", DataType.FloatVector) { Dimension = 2 }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["title"] = "kept", ["embedding"] = new[] { 1f, 2f } },
            new Dictionary<string, object?> { ["id"] = 2L, ["embedding"] = new[] { 3f, 4f } }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData title = columns.First(c => c.FieldName == "title");
        Assert.Equal("kept", title.GetValueAsObject(0));
        Assert.Equal("N/A", title.GetValueAsObject(1));
    }
}
