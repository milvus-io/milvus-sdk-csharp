using Xunit;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class StructFieldDataTests
{
    private static IReadOnlyList<FieldSchema> SubFields()
        => new List<FieldSchema>
        {
            new("int32", DataType.Int32),
            new("varchar", DataType.VarChar) { MaxLength = 16 },
            new("vector", DataType.FloatVector) { Dimension = 2 }
        };

    [Fact]
    public void ToGrpc_encodes_struct_field_as_array_of_struct()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["int32"] = 1, ["varchar"] = "a", ["vector"] = new[] { 1f, 2f } },
                    new Dictionary<string, object?> { ["int32"] = 2, ["varchar"] = "b", ["vector"] = new[] { 3f, 4f } }
                },
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["int32"] = 3, ["varchar"] = "c", ["vector"] = new[] { 5f, 6f } }
                }
            },
            SubFields());

        Milvus.Client.Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfStruct, grpc.Type);
        Assert.Equal("st", grpc.FieldName);
        Assert.Equal(3, grpc.StructArrays.Fields.Count);

        // int32 sub-field: Array with two rows packed into ArrayArray.
        Milvus.Client.Grpc.FieldData intSub = grpc.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.Array, intSub.Type);
        Assert.Equal(Milvus.Client.Grpc.DataType.Int32, intSub.Scalars.ArrayData.ElementType);
        Assert.Equal(2, intSub.Scalars.ArrayData.Data.Count);
        Assert.Equal(new[] { 1, 2 }, intSub.Scalars.ArrayData.Data[0].IntData.Data.ToArray());
        Assert.Equal(new[] { 3 }, intSub.Scalars.ArrayData.Data[1].IntData.Data.ToArray());

        // varchar sub-field.
        Milvus.Client.Grpc.FieldData varcharSub = grpc.StructArrays.Fields[1];
        Assert.Equal(new[] { "a", "b" }, varcharSub.Scalars.ArrayData.Data[0].StringData.Data.ToArray());
        Assert.Equal(new[] { "c" }, varcharSub.Scalars.ArrayData.Data[1].StringData.Data.ToArray());

        // vector sub-field: ArrayOfVector, each row one packed VectorField (dim=2).
        Milvus.Client.Grpc.FieldData vectorSub = grpc.StructArrays.Fields[2];
        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfVector, vectorSub.Type);
        Assert.Equal(Milvus.Client.Grpc.DataType.FloatVector, vectorSub.Vectors.VectorArray.ElementType);
        Assert.Equal(2, vectorSub.Vectors.VectorArray.Data.Count);
        Assert.Equal(new[] { 1f, 2f, 3f, 4f }, vectorSub.Vectors.VectorArray.Data[0].FloatVector.Data.ToArray());
        Assert.Equal(new[] { 5f, 6f }, vectorSub.Vectors.VectorArray.Data[1].FloatVector.Data.ToArray());
    }

    [Fact]
    public void ToGrpc_encodes_sparse_vector_sub_field()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?>
                    {
                        ["vector"] = new SparseVector<float>(new[] { 0, 2 }, new[] { 0.1f, 0.2f })
                    }
                }
            },
            new List<FieldSchema> { new("vector", DataType.SparseFloatVector) });

        Milvus.Client.Grpc.FieldData grpc = field.ToGrpcFieldData();

        Milvus.Client.Grpc.FieldData vectorSub = grpc.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfVector, vectorSub.Type);
        Milvus.Client.Grpc.VectorArray vectorArray = vectorSub.Vectors.VectorArray;
        Assert.Equal(Milvus.Client.Grpc.DataType.SparseFloatVector, vectorArray.ElementType);
        Assert.Equal(3, vectorArray.Dim); // max index (2) + 1
        Milvus.Client.Grpc.VectorField singleRow = Assert.Single(vectorArray.Data);
        Assert.Single(singleRow.SparseFloatVector.Contents);
    }

    [Fact]
    public void Slice_preserves_sub_field_schema()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["int32"] = 1, ["varchar"] = "a", ["vector"] = new[] { 1f, 2f } }
                },
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["int32"] = 2, ["varchar"] = "b", ["vector"] = new[] { 3f, 4f } }
                }
            },
            SubFields());

        StructFieldData sliced = (StructFieldData)field.Slice(1, 1);

        Assert.Equal(1, sliced.RowCount);
        Assert.Equal("st", sliced.FieldName);
        Assert.Equal(2, (sliced.Data[0]![0])["int32"]);
    }

    [Fact]
    public void RowDataConverter_builds_struct_column_from_rows()
    {
        var schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true)
            },
            StructFields =
            {
                new StructFieldSchema("st")
                {
                    MaxCapacity = 10,
                    Fields =
                    {
                        new FieldSchema("int32", DataType.Int32),
                        new FieldSchema("varchar", DataType.VarChar) { MaxLength = 16 },
                        new FieldSchema("vector", DataType.FloatVector) { Dimension = 2 }
                    }
                }
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?>
            {
                ["st"] = new object[]
                {
                    new Dictionary<string, object?> { ["int32"] = 1, ["varchar"] = "a", ["vector"] = new[] { 1f, 2f } },
                    new Dictionary<string, object?> { ["int32"] = 2, ["varchar"] = "b", ["vector"] = new[] { 3f, 4f } }
                }
            },
            new Dictionary<string, object?>
            {
                ["st"] = new object[]
                {
                    new Dictionary<string, object?> { ["int32"] = 3, ["varchar"] = "c", ["vector"] = new[] { 5f, 6f } }
                }
            }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        StructFieldData structColumn = Assert.IsType<StructFieldData>(Assert.Single(columns));
        Assert.Equal("st", structColumn.FieldName);
        Assert.Equal(2, structColumn.RowCount);
        Assert.Equal(2, structColumn.Data[0]!.Count);
        Assert.Single(structColumn.Data[1]!);
        Assert.Equal("b", structColumn.Data[0]![1]["varchar"]);
    }

    [Fact]
    public void RowDataConverter_unknown_struct_like_field_without_struct_schema_goes_dynamic()
    {
        var schema = new CollectionSchema
        {
            EnableDynamicFields = true,
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true)
            }
        };

        var rows = new[]
        {
            new Dictionary<string, object?> { ["st"] = "not-struct" }
        };

        List<FieldData> columns = RowDataConverter.ConvertRows(rows, schema, isUpsert: false);

        FieldData dynamic = columns.Single(c => c.IsDynamic);
        Assert.Equal("{\"st\":\"not-struct\"}", dynamic.GetValueAsObject(0));
    }

    [Fact]
    public void ToGrpc_encodes_binary_vector_sub_field()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["vector"] = new byte[] { 0x0F, 0xF0 } }
                }
            },
            new List<FieldSchema> { new("vector", DataType.BinaryVector) { Dimension = 16 } });

        Milvus.Client.Grpc.FieldData grpc = field.ToGrpcFieldData();

        Milvus.Client.Grpc.FieldData vectorSub = grpc.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfVector, vectorSub.Type);
        Assert.Equal(Milvus.Client.Grpc.DataType.BinaryVector, vectorSub.Vectors.VectorArray.ElementType);
        Milvus.Client.Grpc.VectorField row = Assert.Single(vectorSub.Vectors.VectorArray.Data);
        Assert.Equal(new byte[] { 0x0F, 0xF0 }, row.BinaryVector.ToByteArray());
    }

    [Fact]
    public void ToGrpc_encodes_float16_sub_field()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["vector"] = new ushort[] { 0x3C00, 0 } }
                }
            },
            new List<FieldSchema> { new("vector", DataType.Float16Vector) { Dimension = 2 } });

        Milvus.Client.Grpc.FieldData grpc = field.ToGrpcFieldData();

        Milvus.Client.Grpc.FieldData vectorSub = grpc.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.Float16Vector, vectorSub.Vectors.VectorArray.ElementType);
        Milvus.Client.Grpc.VectorField row = Assert.Single(vectorSub.Vectors.VectorArray.Data);
        Assert.Equal(4, row.Float16Vector.Length); // 2 ushorts * 2 bytes
    }

    [Fact]
    public void ToGrpc_encodes_bfloat16_sub_field()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["vector"] = new ushort[] { Float16Utils.FloatToBf16(1f), 0 } }
                }
            },
            new List<FieldSchema> { new("vector", DataType.BFloat16Vector) { Dimension = 2 } });

        Milvus.Client.Grpc.FieldData grpc = field.ToGrpcFieldData();

        Milvus.Client.Grpc.FieldData vectorSub = grpc.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.Bfloat16Vector, vectorSub.Vectors.VectorArray.ElementType);
        Milvus.Client.Grpc.VectorField row = Assert.Single(vectorSub.Vectors.VectorArray.Data);
        Assert.Equal(4, row.Bfloat16Vector.Length); // 2 ushorts * 2 bytes
    }

    [Fact]
    public void ToGrpc_encodes_int8_vector_sub_field()
    {
        var field = new StructFieldData(
            "st",
            new IReadOnlyList<IDictionary<string, object?>>?[]
            {
                new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["vector"] = new sbyte[] { 1, 2, 3, 4 } }
                }
            },
            new List<FieldSchema> { new("vector", DataType.Int8Vector) { Dimension = 4 } });

        Milvus.Client.Grpc.FieldData grpc = field.ToGrpcFieldData();

        Milvus.Client.Grpc.FieldData vectorSub = grpc.StructArrays.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.Int8Vector, vectorSub.Vectors.VectorArray.ElementType);
        Milvus.Client.Grpc.VectorField row = Assert.Single(vectorSub.Vectors.VectorArray.Data);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, row.Int8Vector.ToByteArray());
    }
}
