using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class DqlConversionsTests
{
    [Fact]
    public void ProcessReturnedFieldData_reconstructs_null_rows_from_valid_data()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "age",
            Type = Grpc.DataType.Int64
        };
        field.Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray() };
        field.Scalars.LongData.Data.Add(25);
        field.Scalars.LongData.Data.Add(40);
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData<long?> age = Assert.IsType<FieldData<long?>>(decoded[0]);
        Assert.Equal(3, age.RowCount);
        Assert.Equal(25L, age.Data[0]);
        Assert.Null(age.Data[1]);
        Assert.Equal(40L, age.Data[2]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_positional_scalar_with_null_rows()
    {
        // Real servers send scalar columns positionally: one data slot per row (zero-filled for null rows)
        // plus a same-length valid_data mask. validData.Count == data.Count selects the positional branch.
        var field = new Grpc.FieldData
        {
            FieldName = "age",
            Type = Grpc.DataType.Int64
        };
        field.Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray() };
        field.Scalars.LongData.Data.Add(25);
        field.Scalars.LongData.Data.Add(0);   // null row, zero-filled
        field.Scalars.LongData.Data.Add(40);
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData<long?> age = Assert.IsType<FieldData<long?>>(decoded[0]);
        Assert.Equal(3, age.RowCount);
        // Positional masking: each row's own slot is kept for valid rows, null substituted for invalid ones.
        Assert.Equal(25L, age.Data[0]);
        Assert.Null(age.Data[1]);
        Assert.Equal(40L, age.Data[2]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_strings_with_null_rows()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "name",
            Type = Grpc.DataType.VarChar
        };
        field.Scalars = new Grpc.ScalarField { StringData = new Grpc.StringArray() };
        field.Scalars.StringData.Data.Add("alice");
        field.Scalars.StringData.Data.Add("carol");
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData<string> name = Assert.IsType<FieldData<string>>(decoded[0]);
        Assert.Equal(3, name.RowCount);
        Assert.Equal("alice", name.Data[0]);
        Assert.Null(name.Data[1]);
        Assert.Equal("carol", name.Data[2]);
    }

    [Theory]
    [InlineData(Milvus.Client.V2.Types.DataType.Int8)]
    [InlineData(Milvus.Client.V2.Types.DataType.Int16)]
    [InlineData(Milvus.Client.V2.Types.DataType.Int32)]
    public void ProcessReturnedFieldData_preserves_int_width(Milvus.Client.V2.Types.DataType type)
    {
        var field = new Grpc.FieldData
        {
            FieldName = "count",
            Type = (Grpc.DataType)(int)type
        };
        field.Scalars = new Grpc.ScalarField { IntData = new Grpc.IntArray() };
        field.Scalars.IntData.Data.Add(1);
        field.Scalars.IntData.Data.Add(2);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        FieldData decodedField = decoded[0];
        Assert.Equal(type, decodedField.DataType);
    }

    [Fact]
    public void ProcessReturnedFieldData_reconstructs_null_array_rows_from_valid_data()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "tags",
            Type = Grpc.DataType.Array
        };
        field.Scalars = new Grpc.ScalarField { ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.VarChar } };
        var row0 = new Grpc.ScalarField { StringData = new Grpc.StringArray() };
        row0.StringData.Data.Add("a");
        row0.StringData.Data.Add("b");
        var row2 = new Grpc.ScalarField { StringData = new Grpc.StringArray() };
        row2.StringData.Data.Add("c");
        field.Scalars.ArrayData.Data.Add(row0);
        field.Scalars.ArrayData.Data.Add(row2);
        field.ValidData.Add(true);
        field.ValidData.Add(false);
        field.ValidData.Add(true);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        ArrayFieldData<string> tags = Assert.IsType<ArrayFieldData<string>>(decoded[0]);
        Assert.Equal(3, tags.RowCount);
        Assert.Equal(new[] { "a", "b" }, tags.Data[0]);
        Assert.Null(tags.Data[1]);
        Assert.Equal(new[] { "c" }, tags.Data[2]);
    }

    [Theory]
    [InlineData(DataType.Int8, typeof(ArrayFieldData<sbyte>))]
    [InlineData(DataType.Int16, typeof(ArrayFieldData<short>))]
    [InlineData(DataType.Int32, typeof(ArrayFieldData<int>))]
    public void ProcessReturnedFieldData_preserves_array_element_width(DataType elementType, Type expectedType)
    {
        var field = new Grpc.FieldData
        {
            FieldName = "nums",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = (Grpc.DataType)(int)elementType }
            }
        };
        var row = new Grpc.ScalarField { IntData = new Grpc.IntArray() };
        row.IntData.Data.Add(1);
        field.Scalars.ArrayData.Data.Add(row);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        Assert.IsType(expectedType, decoded[0]);
    }

    [Fact]
    public void ProcessReturnedFieldData_struct_vector_degrades_when_dimension_unset()
    {
        // A struct vector sub-field whose dim is unset (0) must degrade to an empty row instead of throwing
        // DivideByZeroException in the chunking helpers.
        var structField = new Grpc.FieldData
        {
            FieldName = "st",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var vecSub = new Grpc.FieldData { FieldName = "vec", Type = Grpc.DataType.ArrayOfVector };
        vecSub.Vectors = new Grpc.VectorField();
        vecSub.Vectors.VectorArray = new Grpc.VectorArray { ElementType = Grpc.DataType.Float16Vector };
        var packed = new Grpc.VectorField { Dim = 2 };
        packed.Float16Vector = Google.Protobuf.ByteString.CopyFrom(new byte[] { 0x00, 0x3C, 0x00, 0x40 });
        vecSub.Vectors.VectorArray.Data.Add(packed);
        structField.StructArrays.Fields.Add(vecSub);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(structField);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        var structData = Assert.IsType<StructFieldData>(decoded[0]);
        Assert.Equal(1, structData.RowCount);
        Assert.Single(structData.Data);
        Assert.Empty(structData.Data[0]!);
    }

    [Fact]
    public void ProcessReturnedFieldData_struct_empty_scalar_row_does_not_throw()
    {
        // A struct scalar sub-field row with no packed data (oneof DataCase None) must decode to an empty row
        // instead of throwing NullReferenceException in the typed accessors.
        var structField = new Grpc.FieldData
        {
            FieldName = "st",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var intSub = new Grpc.FieldData { FieldName = "int32", Type = Grpc.DataType.Array };
        intSub.Scalars = new Grpc.ScalarField { ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Int32 } };
        // One row with an empty ScalarField (no int_data packed).
        intSub.Scalars.ArrayData.Data.Add(new Grpc.ScalarField());
        structField.StructArrays.Fields.Add(intSub);

        var query = new Grpc.QueryResults();
        query.FieldsData.Add(structField);
        List<FieldData> decoded = DqlConversions.ProcessReturnedFieldData(query.FieldsData);

        var structData = Assert.IsType<StructFieldData>(decoded[0]);
        Assert.Single(structData.Data);
        Assert.Empty(structData.Data[0]!);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_float_vector()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "vec",
            Type = Grpc.DataType.FloatVector,
            Vectors = new Grpc.VectorField
            {
                Dim = 2,
                FloatVector = new Grpc.FloatArray()
            }
        };
        field.Vectors.FloatVector.Data.Add(1f);
        field.Vectors.FloatVector.Data.Add(2f);
        field.Vectors.FloatVector.Data.Add(3f);
        field.Vectors.FloatVector.Data.Add(4f);

        FieldData decoded = Decode(field);
        Assert.IsType<FloatVectorFieldData>(decoded);
        Assert.Equal(new[] { 1f, 2f }, ((ReadOnlyMemory<float>)decoded.GetValueAsObject(0)!).ToArray());
        Assert.Equal(new[] { 3f, 4f }, ((ReadOnlyMemory<float>)decoded.GetValueAsObject(1)!).ToArray());
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_binary_vector()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "bin",
            Type = Grpc.DataType.BinaryVector,
            Vectors = new Grpc.VectorField
            {
                Dim = 16,
                BinaryVector = Google.Protobuf.ByteString.CopyFrom(new byte[] { 0x0F, 0xF0, 0xFF, 0x00 })
            }
        };

        FieldData decoded = Decode(field);
        Assert.Equal(new byte[] { 0x0F, 0xF0 }, ((ReadOnlyMemory<byte>)decoded.GetValueAsObject(0)!).ToArray());
        Assert.Equal(new byte[] { 0xFF, 0x00 }, ((ReadOnlyMemory<byte>)decoded.GetValueAsObject(1)!).ToArray());
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_float16_vector()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "fp16",
            Type = Grpc.DataType.Float16Vector,
            Vectors = new Grpc.VectorField
            {
                Dim = 2,
                Float16Vector = Google.Protobuf.ByteString.CopyFrom(
                    BitConverter.GetBytes((ushort)0x3C00) // 1.0 in FP16
                        .Concat(BitConverter.GetBytes((ushort)0))
                        .ToArray())
            }
        };

        FieldData decoded = Decode(field);
        Assert.Equal((ushort)0x3C00, ((ReadOnlyMemory<ushort>)decoded.GetValueAsObject(0)!).Span[0]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_int8_vector()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "i8",
            Type = Grpc.DataType.Int8Vector,
            Vectors = new Grpc.VectorField
            {
                Dim = 2,
                Int8Vector = Google.Protobuf.ByteString.CopyFrom(new[] { (byte)1, (byte)2, (byte)3, (byte)4 })
            }
        };

        FieldData decoded = Decode(field);
        Assert.Equal(new sbyte[] { 1, 2 }, ((ReadOnlyMemory<sbyte>)decoded.GetValueAsObject(0)!).ToArray());
        Assert.Equal(new sbyte[] { 3, 4 }, ((ReadOnlyMemory<sbyte>)decoded.GetValueAsObject(1)!).ToArray());
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_timestamptz_native_slot()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "ts",
            Type = Grpc.DataType.Timestamptz,
            Scalars = new Grpc.ScalarField { TimestamptzData = new Grpc.TimestamptzArray() }
        };
        // 2025-01-01T00:00:00Z in epoch microseconds.
        field.Scalars.TimestamptzData.Data.Add(1735689600_000000L);
        field.Scalars.TimestamptzData.Data.Add(1735689601_000000L);

        FieldData decoded = Decode(field);
        Assert.Equal("2025-01-01T00:00:00.0000000+00:00", decoded.GetValueAsObject(0));
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_json_column()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "meta",
            Type = Grpc.DataType.Json,
            Scalars = new Grpc.ScalarField { JsonData = new Grpc.JSONArray() }
        };
        field.Scalars.JsonData.Data.Add(Google.Protobuf.ByteString.CopyFromUtf8("{\"a\":1}"));

        FieldData decoded = Decode(field);
        Assert.Equal("{\"a\":1}", decoded.GetValueAsObject(0));
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_geometry_wkt()
    {
        var field = new Grpc.FieldData
        {
            FieldName = "geo",
            Type = Grpc.DataType.Geometry,
            Scalars = new Grpc.ScalarField { GeometryWktData = new Grpc.GeometryWktArray() }
        };
        field.Scalars.GeometryWktData.Data.Add("POINT(1 1)");

        FieldData decoded = Decode(field);
        Assert.Equal(DataType.Geometry, decoded.DataType);
        Assert.Equal("POINT(1 1)", decoded.GetValueAsObject(0));
    }

    private static FieldData Decode(Grpc.FieldData field)
    {
        var query = new Grpc.QueryResults();
        query.FieldsData.Add(field);
        return DqlConversions.ProcessReturnedFieldData(query.FieldsData)[0];
    }

    [Fact]
    public void ProcessReturnedFieldData_struct_preserves_vector_dimension_for_round_trip()
    {
        // A struct vector sub-field decoded with its wire dimension must re-encode with that Dim (the
        // query->reinsert round trip would otherwise degrade to Dim=0 and drop the vectors).
        var structField = new Grpc.FieldData
        {
            FieldName = "st",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var vecSub = new Grpc.FieldData
        {
            FieldName = "vector",
            Type = Grpc.DataType.ArrayOfVector,
            Vectors = new Grpc.VectorField
            {
                Dim = 2,
                VectorArray = new Grpc.VectorArray { Dim = 2, ElementType = Grpc.DataType.FloatVector }
            }
        };
        vecSub.Vectors.VectorArray.Data.Add(new Grpc.VectorField { Dim = 2 });
        vecSub.Vectors.VectorArray.Data[0].FloatVector = new Grpc.FloatArray();
        vecSub.Vectors.VectorArray.Data[0].FloatVector.Data.Add(1f);
        vecSub.Vectors.VectorArray.Data[0].FloatVector.Data.Add(2f);
        structField.StructArrays.Fields.Add(vecSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        Assert.Equal(1, structData.RowCount);

        Grpc.FieldData reencoded = structData.ToGrpcFieldData();
        Grpc.FieldData reVecSub = reencoded.StructArrays.Fields[0];
        Assert.Equal(Grpc.DataType.ArrayOfVector, reVecSub.Type);
        Assert.Equal(2, reVecSub.Vectors.VectorArray.Dim);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_element_nullable_struct_scalar_row()
    {
        // A struct scalar sub-field with element-level null: the row cell's valid_data marks which elements
        // are null and the typed payload is compacted to the non-null values.
        var structField = new Grpc.FieldData
        {
            FieldName = "metadata",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var scoreSub = new Grpc.FieldData
        {
            FieldName = "score",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Int32 }
            }
        };
        var rowCell = new Grpc.ScalarField();
        rowCell.IntData = new Grpc.IntArray();
        rowCell.IntData.Data.Add(10);
        rowCell.IntData.Data.Add(30);
        rowCell.ValidData.Add(true);
        rowCell.ValidData.Add(false);
        rowCell.ValidData.Add(true);
        scoreSub.Scalars.ArrayData.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(scoreSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        Assert.Equal(1, structData.RowCount);

        IReadOnlyList<IDictionary<string, object?>> elements = structData.Data[0]!;
        Assert.Equal(3, elements.Count);
        Assert.Equal(10, elements[0]["score"]);
        Assert.Null(elements[1]["score"]);
        Assert.Equal(30, elements[2]["score"]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_element_nullable_struct_vector_row()
    {
        // A struct vector sub-field with element-level null: null vectors are compacted out and the cell's
        // valid_data marks their positions.
        var structField = new Grpc.FieldData
        {
            FieldName = "metadata",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var embSub = new Grpc.FieldData
        {
            FieldName = "emb",
            Type = Grpc.DataType.ArrayOfVector,
            Vectors = new Grpc.VectorField
            {
                Dim = 2,
                VectorArray = new Grpc.VectorArray { Dim = 2, ElementType = Grpc.DataType.FloatVector }
            }
        };
        var rowCell = new Grpc.VectorField { Dim = 2 };
        rowCell.FloatVector = new Grpc.FloatArray();
        rowCell.FloatVector.Data.Add(1f);
        rowCell.FloatVector.Data.Add(2f);
        rowCell.FloatVector.Data.Add(3f);
        rowCell.FloatVector.Data.Add(4f);
        rowCell.ValidData.Add(true);
        rowCell.ValidData.Add(false);
        rowCell.ValidData.Add(true);
        embSub.Vectors.VectorArray.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(embSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        IReadOnlyList<IDictionary<string, object?>> elements = structData.Data[0]!;
        Assert.Equal(3, elements.Count);
        Assert.Equal(new[] { 1f, 2f }, ((ReadOnlyMemory<float>)elements[0]["emb"]!).ToArray());
        Assert.Null(elements[1]["emb"]);
        Assert.Equal(new[] { 3f, 4f }, ((ReadOnlyMemory<float>)elements[2]["emb"]!).ToArray());
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_dense_element_nullable_struct_scalar_row()
    {
        // Real servers return struct element payloads densely: one slot per element (null values zero-filled)
        // with a same-length valid_data mask. The dense branch must mask positionally rather than treat the
        // payload as compacted.
        var structField = new Grpc.FieldData
        {
            FieldName = "metadata",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var scoreSub = new Grpc.FieldData
        {
            FieldName = "score",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Int32 }
            }
        };
        var rowCell = new Grpc.ScalarField();
        rowCell.IntData = new Grpc.IntArray();
        rowCell.IntData.Data.Add(10);
        rowCell.IntData.Data.Add(0); // null element, zero-filled placeholder
        rowCell.IntData.Data.Add(30);
        rowCell.ValidData.Add(true);
        rowCell.ValidData.Add(false);
        rowCell.ValidData.Add(true);
        scoreSub.Scalars.ArrayData.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(scoreSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        IReadOnlyList<IDictionary<string, object?>> elements = structData.Data[0]!;
        Assert.Equal(3, elements.Count);
        Assert.Equal(10, elements[0]["score"]);
        Assert.Null(elements[1]["score"]);
        Assert.Equal(30, elements[2]["score"]);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_dense_struct_nested_array_sub_field()
    {
        // Real servers return nested-Array sub-fields densely: one leaf per logical element, with a null inner
        // array carried as an empty placeholder leaf and a same-length valid_data mask.
        var structField = new Grpc.FieldData
        {
            FieldName = "metadata",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var tagsSub = new Grpc.FieldData
        {
            FieldName = "tags",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Array }
            }
        };
        var rowCell = new Grpc.ScalarField
        {
            ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.VarChar }
        };
        var leaf0 = new Grpc.ScalarField();
        leaf0.StringData = new Grpc.StringArray();
        leaf0.StringData.Data.Add("a");
        leaf0.ValidData.Add(true);
        leaf0.ValidData.Add(false);
        var leaf1 = new Grpc.ScalarField(); // null inner array: empty placeholder leaf
        var leaf2 = new Grpc.ScalarField();
        leaf2.StringData = new Grpc.StringArray();
        leaf2.StringData.Data.Add("b");
        rowCell.ArrayData.Data.Add(leaf0);
        rowCell.ArrayData.Data.Add(leaf1);
        rowCell.ArrayData.Data.Add(leaf2);
        rowCell.ValidData.Add(true);
        rowCell.ValidData.Add(false);
        rowCell.ValidData.Add(true);
        tagsSub.Scalars.ArrayData.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(tagsSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        IReadOnlyList<IDictionary<string, object?>> elements = structData.Data[0]!;
        Assert.Equal(3, elements.Count);
        Assert.Equal(new object?[] { "a", null }, ((IEnumerable<object?>)elements[0]["tags"]!).Cast<object?>().ToArray());
        Assert.Null(elements[1]["tags"]);
        Assert.Equal(new object?[] { "b" }, ((IEnumerable<object?>)elements[2]["tags"]!).Cast<object?>().ToArray());
    }

    [Fact]
    public void ProcessReturnedFieldData_strips_struct_sub_field_path_key()
    {
        // A sub-field added via addCollectionField("clips[note]") may be echoed back with the "clips[" prefix
        // in its wire field name; the decoded struct element map keys must be stripped back to "note"
        // (mirroring pymilvus/Java) so they match the plain names users inserted with.
        var structField = new Grpc.FieldData
        {
            FieldName = "clips",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var noteSub = new Grpc.FieldData
        {
            FieldName = "clips[note]",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.VarChar }
            }
        };
        var rowCell = new Grpc.ScalarField();
        rowCell.StringData = new Grpc.StringArray();
        rowCell.StringData.Data.Add("hello");
        noteSub.Scalars.ArrayData.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(noteSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        IReadOnlyList<IDictionary<string, object?>> elements = structData.Data[0]!;
        Assert.Equal("hello", elements[0]["note"]);
        Assert.False(elements[0].ContainsKey("clips[note]"));
    }

    [Fact]
    public void ProcessReturnedFieldData_struct_nested_array_recovers_element_type_from_later_cell()
    {
        // A nested-Array sub-field whose first row cell carries no array payload (an all-null inner-array row,
        // oneof DataCase None) must still recover its leaf element type from a later cell, so a
        // query-then-reinsert round trip does not throw NullReferenceException in EncodeRowNestedArray.
        var structField = new Grpc.FieldData
        {
            FieldName = "metadata",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var tagsSub = new Grpc.FieldData
        {
            FieldName = "tags",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Array }
            }
        };
        var emptyCell = new Grpc.ScalarField(); // all-null inner arrays: no array payload
        emptyCell.ValidData.Add(false);
        var rowCell = new Grpc.ScalarField
        {
            ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.VarChar }
        };
        var leaf = new Grpc.ScalarField();
        leaf.StringData = new Grpc.StringArray();
        leaf.StringData.Data.Add("a");
        rowCell.ArrayData.Data.Add(leaf);
        rowCell.ValidData.Add(true);
        tagsSub.Scalars.ArrayData.Data.Add(emptyCell);
        tagsSub.Scalars.ArrayData.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(tagsSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);

        // Re-encode must not throw (ElementDataType recovered from the second cell), and the recovered leaf
        // type must be encoded into the row cell's inner ArrayData.
        Grpc.FieldData reencoded = structData.ToGrpcFieldData();
        Grpc.ScalarField reRowCell = reencoded.StructArrays.Fields[0].Scalars.ArrayData.Data[1];
        Assert.Equal(Grpc.DataType.VarChar, reRowCell.ArrayData.ElementType);
    }

    [Fact]
    public void ProcessReturnedFieldData_decodes_struct_nested_array_sub_field()
    {
        // A nested Array sub-field: the row cell is itself an ArrayArray of leaf values, with its own valid_data
        // marking null inner arrays and each leaf's valid_data marking null values inside the inner array.
        var structField = new Grpc.FieldData
        {
            FieldName = "metadata",
            Type = Grpc.DataType.ArrayOfStruct,
            StructArrays = new Grpc.StructArrayField()
        };
        var tagsSub = new Grpc.FieldData
        {
            FieldName = "tags",
            Type = Grpc.DataType.Array,
            Scalars = new Grpc.ScalarField
            {
                ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.Array }
            }
        };
        var rowCell = new Grpc.ScalarField
        {
            ArrayData = new Grpc.ArrayArray { ElementType = Grpc.DataType.VarChar }
        };
        var leaf0 = new Grpc.ScalarField();
        leaf0.StringData = new Grpc.StringArray();
        leaf0.StringData.Data.Add("a");
        leaf0.ValidData.Add(true);
        leaf0.ValidData.Add(false);
        var leaf2 = new Grpc.ScalarField();
        leaf2.StringData = new Grpc.StringArray();
        leaf2.StringData.Data.Add("b");
        rowCell.ArrayData.Data.Add(leaf0);
        rowCell.ArrayData.Data.Add(leaf2);
        rowCell.ValidData.Add(true);
        rowCell.ValidData.Add(false);
        rowCell.ValidData.Add(true);
        tagsSub.Scalars.ArrayData.Data.Add(rowCell);
        structField.StructArrays.Fields.Add(tagsSub);

        FieldData decoded = Decode(structField);
        var structData = Assert.IsType<StructFieldData>(decoded);
        IReadOnlyList<IDictionary<string, object?>> elements = structData.Data[0]!;
        Assert.Equal(3, elements.Count);
        // Element 0: ["a", null] (leaf validity marks the trailing null); element 1: null inner array;
        // element 2: ["b"].
        Assert.Equal(new object?[] { "a", null }, ((IEnumerable<object?>)elements[0]["tags"]!).Cast<object?>().ToArray());
        Assert.Null(elements[1]["tags"]);
        Assert.Equal(new object?[] { "b" }, ((IEnumerable<object?>)elements[2]["tags"]!).Cast<object?>().ToArray());

        // The recovered sub-field schema carries ElementNullable (leaf nullability) from the wire, so a
        // query-then-reinsert round trip re-encodes the null leaf without throwing.
        Grpc.FieldData reencoded = structData.ToGrpcFieldData();
        Grpc.FieldData reTags = reencoded.StructArrays.Fields.Single(f => f.FieldName == "tags");
        Grpc.ScalarField reRowCell = Assert.Single(reTags.Scalars.ArrayData.Data);
        Assert.Equal(new[] { true, false, true }, reRowCell.ValidData);
        Assert.Equal(new[] { "a" }, reRowCell.ArrayData.Data[0].StringData.Data);
        Assert.Equal(new[] { true, false }, reRowCell.ArrayData.Data[0].ValidData);
    }
}
