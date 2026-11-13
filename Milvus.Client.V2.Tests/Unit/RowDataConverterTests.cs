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
    public void ConvertRows_encodes_element_nullable_struct_scalar_sub_field()
    {
        // An element-nullable scalar sub-field encodes null elements compacted out with per-element valid_data
        // on the row cell (mirroring pymilvus's compacted encoding and the Java/C++ SDKs).
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields = { new FieldSchema("score", DataType.Int32) { Nullable = true } }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?> { ["score"] = 1 },
                    new Dictionary<string, object?> { ["score"] = null },
                    new Dictionary<string, object?> { ["score"] = 3 }
                }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));

        Grpc.FieldData grpc = metadata.ToGrpcFieldData();
        Grpc.FieldData score = grpc.StructArrays.Fields.Single(f => f.FieldName == "score");
        Assert.Equal(Grpc.DataType.Array, score.Type);
        Grpc.ScalarField rowCell = Assert.Single(score.Scalars.ArrayData.Data);
        // Only the non-null values are stored; valid_data marks the null position.
        Assert.Equal(new[] { 1, 3 }, rowCell.IntData.Data);
        Assert.Equal(new[] { true, false, true }, rowCell.ValidData);
    }

    [Fact]
    public void ConvertRows_encodes_struct_nested_array_sub_field()
    {
        // A nested Array sub-field encodes recursively: the row cell is an ArrayArray of leaf values with its
        // own valid_data marking null inner arrays, and each leaf carries its own valid_data for null values
        // inside the inner array.
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields =
                    {
                        new FieldSchema("tags", DataType.Array)
                        {
                            ElementDataType = DataType.VarChar,
                            MaxCapacity = 8,
                            Nullable = true,
                            ElementNullable = true
                        }
                    }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?> { ["tags"] = new object?[] { "a", null } },
                    new Dictionary<string, object?> { ["tags"] = null },
                    new Dictionary<string, object?> { ["tags"] = new object?[] { "b" } }
                }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));

        Grpc.FieldData grpc = metadata.ToGrpcFieldData();
        Grpc.FieldData tags = grpc.StructArrays.Fields.Single(f => f.FieldName == "tags");
        Assert.Equal(Grpc.DataType.Array, tags.Type);
        // The outer ArrayArray element type is the sub-field's own Array type.
        Assert.Equal(Grpc.DataType.Array, tags.Scalars.ArrayData.ElementType);
        Grpc.ScalarField rowCell = Assert.Single(tags.Scalars.ArrayData.Data);
        // Element-level validity: ["a", null] non-null, null inner array invalid, ["b"] non-null.
        Assert.Equal(new[] { true, false, true }, rowCell.ValidData);
        Assert.Equal(Grpc.DataType.VarChar, rowCell.ArrayData.ElementType);
        Assert.Equal(2, rowCell.ArrayData.Data.Count);
        // First inner array ["a", null]: compacted to ["a"] with leaf validity [true, false].
        Grpc.ScalarField leaf0 = rowCell.ArrayData.Data[0];
        Assert.Equal(new[] { "a" }, leaf0.StringData.Data);
        Assert.Equal(new[] { true, false }, leaf0.ValidData);
        // Second inner array ["b"]: the leaf is element-nullable, so the validity bitmap is always written
        // (matching pymilvus's compacted encoding) even though every value is present.
        Grpc.ScalarField leaf2 = rowCell.ArrayData.Data[1];
        Assert.Equal(new[] { "b" }, leaf2.StringData.Data);
        Assert.Equal(new[] { true }, leaf2.ValidData);
    }

    [Fact]
    public void ConvertRows_rejects_null_on_non_element_nullable_sub_field()
    {
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields = { new FieldSchema("score", DataType.Int32) }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?> { ["score"] = null }
                }
            }
        };

        // The null element is rejected when the sub-field is not element-nullable. ConvertRows builds the
        // columnar data; the rejection surfaces when the struct data is encoded to the wire.
        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));
        Assert.Throws<ArgumentException>(() => metadata.ToGrpcFieldData());
    }

    [Fact]
    public void ConvertRows_dual_writes_row_validity_mask_to_sub_field_payload()
    {
        // A null struct row is compacted out and flagged in the sub-field FieldData.valid_data; schema.proto
        // requires the same row-validity mask to be dual-written to the payload ScalarField/VectorField
        // valid_data (mirroring pymilvus's sync_row_validity).
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("st")
                {
                    MaxCapacity = 8,
                    Nullable = true,
                    Fields = { new FieldSchema("score", DataType.Int32) { Nullable = true } }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["st"] = new[] { new Dictionary<string, object?> { ["score"] = 1 } }
            },
            new Dictionary<string, object?> { ["id"] = 2L, ["st"] = null }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var structField = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "st"));
        Grpc.FieldData grpc = structField.ToGrpcFieldData();
        Grpc.FieldData score = grpc.StructArrays.Fields.Single();
        Assert.Equal(new[] { true, false }, score.ValidData);
        Assert.Equal(new[] { true, false }, score.Scalars.ValidData);
        // Only the valid row is packed.
        Assert.Single(score.Scalars.ArrayData.Data);
    }

    [Fact]
    public void ConvertRows_encodes_element_nullable_struct_vector_sub_field()
    {
        // An element-nullable vector sub-field compacts null vectors out and marks their positions through the
        // row cell's valid_data (mirroring the scalar branch and the C++/Java SDKs).
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields = { new FieldSchema("emb", DataType.FloatVector) { Dimension = 2, Nullable = true } }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?> { ["emb"] = new[] { 1f, 2f } },
                    new Dictionary<string, object?> { ["emb"] = null },
                    new Dictionary<string, object?> { ["emb"] = new[] { 3f, 4f } }
                }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));

        Grpc.FieldData grpc = metadata.ToGrpcFieldData();
        Grpc.FieldData emb = grpc.StructArrays.Fields.Single(f => f.FieldName == "emb");
        Assert.Equal(Grpc.DataType.ArrayOfVector, emb.Type);
        Grpc.VectorField rowCell = Assert.Single(emb.Vectors.VectorArray.Data);
        // Only the non-null vectors are stored; valid_data marks the null position.
        Assert.Equal(new[] { 1f, 2f, 3f, 4f }, rowCell.FloatVector.Data);
        Assert.Equal(new[] { true, false, true }, rowCell.ValidData);
    }

    [Fact]
    public void ConvertRows_rejects_null_on_non_element_nullable_vector_sub_field()
    {
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields = { new FieldSchema("emb", DataType.FloatVector) { Dimension = 2 } }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[] { new Dictionary<string, object?> { ["emb"] = null } }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));
        Assert.Throws<ArgumentException>(() => metadata.ToGrpcFieldData());
    }

    [Fact]
    public void ConvertRows_rejects_null_leaf_on_non_element_nullable_nested_array_sub_field()
    {
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields =
                    {
                        new FieldSchema("tags", DataType.Array)
                        {
                            ElementDataType = DataType.Int64,
                            MaxCapacity = 8,
                            Nullable = true,
                            ElementNullable = false
                        }
                    }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?> { ["tags"] = new object?[] { 1L, null } }
                }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));
        Assert.Throws<ArgumentException>(() => metadata.ToGrpcFieldData());
    }

    [Fact]
    public void ConvertRows_rejects_struct_element_with_unknown_field()
    {
        // A struct element carrying a key that is not a declared sub-field must be rejected (mirroring
        // pymilvus's "unexpected fields" check) instead of being silently ignored on the wire.
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields = { new FieldSchema("score", DataType.Int32) }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[] { new Dictionary<string, object?> { ["score"] = 1, ["typo"] = 2 } }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);
        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));
        ArgumentException ex = Assert.Throws<ArgumentException>(() => metadata.ToGrpcFieldData());
        Assert.Contains("unexpected field 'typo'", ex.Message);
    }

    [Fact]
    public void ConvertRows_rejects_null_struct_row_when_struct_not_nullable()
    {
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields = { new FieldSchema("score", DataType.Int32) }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["id"] = 1L, ["metadata"] = null }
        };

        // A null struct row is rejected up front when the struct is not nullable (mirroring pymilvus), instead
        // of sending a wire payload the server would reject with an opaque error.
        ArgumentException ex = Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(rows, schema, isUpsert: false));
        Assert.Contains("not nullable", ex.Message);
    }

    [Fact]
    public void ConvertRows_path_replace_filters_struct_sub_fields_and_validates_operand()
    {
        var schema = new CollectionSchema
        {
            Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
            StructFields =
            {
                new StructFieldSchema("metadata")
                {
                    MaxCapacity = 8,
                    Fields =
                    {
                        new FieldSchema("score", DataType.Int32),
                        new FieldSchema("tags", DataType.Array)
                        {
                            ElementDataType = DataType.Int64,
                            MaxCapacity = 8,
                            Nullable = true,
                            ElementNullable = true
                        }
                    }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?> { ["tags"] = new object?[] { 1L, 2L } }
                }
            }
        };

        // A child PATH_REPLACE emits only the targeted child sub-field and forwards the op path.
        List<FieldData> columns = RowDataConverter.ConvertRows(
            rows, schema, isUpsert: true, isPartialUpdate: true,
            fieldOps: new[]
            {
                new FieldPartialUpdateOp("metadata", FieldPartialUpdateOpType.PathReplace) { Path = "[1][tags]" }
            });

        var metadata = Assert.IsType<StructFieldData>(columns.First(c => c.FieldName == "metadata"));
        Grpc.FieldData grpc = metadata.ToGrpcFieldData();
        Assert.Single(grpc.StructArrays.Fields);
        Assert.Equal("tags", grpc.StructArrays.Fields[0].FieldName);

        // A whole-element [index] path keeps every sub-field (no child filtering); the operand must still
        // provide the non-nullable children.
        var wholeElementRows = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 2L,
                ["metadata"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["score"] = 7,
                        ["tags"] = new object?[] { 3L, 4L }
                    }
                }
            }
        };
        List<FieldData> wholeElementColumns = RowDataConverter.ConvertRows(
            wholeElementRows, schema, isUpsert: true, isPartialUpdate: true,
            fieldOps: new[]
            {
                new FieldPartialUpdateOp("metadata", FieldPartialUpdateOpType.PathReplace) { Path = "[0]" }
            });
        var wholeElementMetadata = Assert.IsType<StructFieldData>(wholeElementColumns.First(c => c.FieldName == "metadata"));
        Grpc.FieldData wholeGrpc = wholeElementMetadata.ToGrpcFieldData();
        Assert.Equal(2, wholeGrpc.StructArrays.Fields.Count);
        Assert.Equal("score", wholeGrpc.StructArrays.Fields[0].FieldName);
        Assert.Equal("tags", wholeGrpc.StructArrays.Fields[1].FieldName);

        // A missing child key in the operand is rejected.
        var missingKey = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[] { new Dictionary<string, object?> { ["score"] = 1 } }
            }
        };
        Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(
            missingKey, schema, isUpsert: true, isPartialUpdate: true,
            fieldOps: new[]
            {
                new FieldPartialUpdateOp("metadata", FieldPartialUpdateOpType.PathReplace) { Path = "[1][tags]" }
            }));

        // A malformed path is rejected.
        var malformed = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1L,
                ["metadata"] = new[] { new Dictionary<string, object?> { ["tags"] = new object?[] { 1L } } }
            }
        };
        Assert.Throws<ArgumentException>(() => RowDataConverter.ConvertRows(
            malformed, schema, isUpsert: true, isPartialUpdate: true,
            fieldOps: new[]
            {
                new FieldPartialUpdateOp("metadata", FieldPartialUpdateOpType.PathReplace) { Path = "tags" }
            }));
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
