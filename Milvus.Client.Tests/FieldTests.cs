using Xunit;

namespace Milvus.Client.Tests;

public class FieldTests
{
    [Fact]
    public void Create_int_field()
    {
        var field = FieldData.Create("Id", new[] { 1, 2, 3 });

        Assert.Equal("Id", field.FieldName);
        Assert.Equal(MilvusDataType.Int32, field.DataType);
        Assert.Equal(3, field.RowCount);
        Assert.Equivalent(new[] { 1, 2, 3 }, field.Data);
    }

    [Fact]
    public void Create_varchar_field()
    {
        var field = FieldData.CreateVarChar("id", new[] { "fsj", "fsd" });

        Assert.Equal("id", field.FieldName);
        Assert.Equal(MilvusDataType.VarChar, field.DataType);
        Assert.Equal(2, field.RowCount);
        Assert.Equivalent(new[] { "fsj", "fsd" }, field.Data);
    }

    [Fact]
    public void Create_binary_vector_field()
    {
        var field = FieldData.CreateFromBytes("byte", new byte[] { 1, 2, 3, 4, 5, 6 }, dimension: 16);

        Assert.Equal(MilvusDataType.BinaryVector, field.DataType);
        Assert.Equal(3, field.RowCount);
        Assert.Equal(3, field.Data.Count);
    }

    [Fact]
    public void CreateBinaryVectorsTest()
    {
        var field = FieldData.CreateBinaryVectors(
            "byte",
            new ReadOnlyMemory<byte>[]
            {
                new byte[] { 1, 2 },
                new byte[] { 3, 4 }
            });

        Assert.Equal(MilvusDataType.BinaryVector, field.DataType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateFloatVectorTest()
    {
        var field = FieldData.CreateFloatVector(
            "vector",
            new ReadOnlyMemory<float>[]
            {
                new[] { 1f, 2f },
                new[] { 3f, 4f }
            });

        Assert.Equal(MilvusDataType.FloatVector, field.DataType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateFloat16VectorTest()
    {
        var field = FieldData.CreateFloat16Vector(
            "vector",
            new ReadOnlyMemory<Half>[]
            {
                new[] { (Half)1f, (Half)2f },
                new[] { (Half)3f, (Half)4f }
            });

        Assert.Equal(MilvusDataType.Float16Vector, field.DataType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateInt8ArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new sbyte[][]
            {
                [1, 2],
                [3, 4],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Int8, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateInt16ArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new short[][]
            {
                [1, 2],
                [3, 4],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Int16, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateInt32ArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new int[][]
            {
                [1, 2],
                [3, 4],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Int32, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateInt64ArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new long[][]
            {
                [1, 2],
                [3, 4],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Int64, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateBoolArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new bool[][]
            {
                [true, false],
                [false, false],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Bool, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateFloatArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new float[][]
            {
                [1, 2],
                [3, 4],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Float, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void CreateDoubleArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new double[][]
            {
                [1, 2],
                [3, 4],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.Double, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    //TODO: differentiate VarChar and String somehow
    [Fact]
    public void CreateVarCharArrayTest()
    {
        var field = FieldData.CreateArray(
            "vector",
            new string[][]
            {
                ["3d4d387208e04a9abe77be65e2b7c7b3", "a5502ddb557047968a70ff69720d2dd2"],
                ["4c246789a91f4b15aa3b26799df61457", "00a23e95823b4f14854ceed5f7059953"],
            });

        Assert.Equal(MilvusDataType.Array, field.DataType);
        Assert.Equal(MilvusDataType.VarChar, field.ElementType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void FieldSchema_Create_with_nullable_value_type_infers_nullable()
    {
        var field = FieldSchema.Create<int?>("nullable_int");
        Assert.True(field.Nullable);
    }

    [Fact]
    public void FieldSchema_Create_with_non_nullable_value_type_infers_non_nullable()
    {
        var field = FieldSchema.Create<int>("non_nullable_int");
        Assert.False(field.Nullable);
    }

    [Fact]
    public void FieldSchema_Create_with_reference_type_defaults_to_non_nullable()
    {
        // In a nullable context, string (without ?) is non-nullable
        // Create<string> should default to nullable=false
        var field = FieldSchema.Create<string>("non_nullable_string");
        Assert.False(field.Nullable);
    }

    [Fact]
    public void FieldSchema_Create_with_reference_type_explicit_nullable_false_sets_non_nullable()
    {
        var field = FieldSchema.Create<string>("non_nullable_string", nullable: false);
        Assert.False(field.Nullable);
    }

    [Fact]
    public void FieldSchema_Create_with_reference_type_explicit_nullable_true_sets_nullable()
    {
        // When explicitly setting nullable: true, the field becomes nullable
        // This is valid for reference types regardless of compile-time annotation
        var field = FieldSchema.Create<string>("nullable_string", nullable: true);
        Assert.True(field.Nullable);
    }

    [Fact]
    public void FieldSchema_Create_with_explicit_nullable_true_sets_nullable()
    {
        var field = FieldSchema.Create<int?>("nullable_int", nullable: true);
        Assert.True(field.Nullable);
    }

    [Fact]
    public void FieldSchema_Create_with_explicit_nullable_false_on_nullable_type_throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FieldSchema.Create<int?>("nullable_int", nullable: false));
        Assert.Equal("nullable", exception.ParamName);
    }

    [Fact]
    public void FieldSchema_Create_with_explicit_nullable_true_on_non_nullable_value_type_throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FieldSchema.Create<int>("non_nullable_int", nullable: true));
        Assert.Equal("nullable", exception.ParamName);
        Assert.Contains("cannot be null", exception.Message);
    }

    [Fact]
    public void FieldSchema_Create_with_explicit_nullable_false_on_nullable_value_type_throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FieldSchema.Create<int?>("int_field", nullable: false));
        Assert.Equal("nullable", exception.ParamName);
        Assert.Contains("nullable but nullable=false was specified", exception.Message);
    }

    [Fact]
    public void FieldSchema_Create_with_explicit_nullable_false_on_reference_type_sets_non_nullable()
    {
        var field = FieldSchema.Create<string>("string_field", nullable: false);
        Assert.False(field.Nullable);
    }

    [Fact]
    public void FieldSchema_CreateVarchar_with_nullable_true_sets_nullable()
    {
        var field = FieldSchema.CreateVarchar("nullable_varchar", maxLength: 100, nullable: true);
        Assert.True(field.Nullable);
    }

    [Fact]
    public void FieldSchema_CreateArray_with_nullable_true_sets_nullable()
    {
        var field = FieldSchema.CreateArray<int>("nullable_array", maxCapacity: 10, nullable: true);
        Assert.True(field.Nullable);
    }

    [Fact]
    public void CreateSparseFloatVectorTest()
    {
        var sparseVectors = new[]
        {
            new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]),
            new MilvusSparseVector<float>((int[])[50, 200], (float[])[3.0f, 4.0f]),
        };

        var field = FieldData.CreateSparseFloatVector("sparse_vector", sparseVectors);

        Assert.Equal("sparse_vector", field.FieldName);
        Assert.Equal(MilvusDataType.SparseFloatVector, field.DataType);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(2, field.Data.Count);
    }

    [Fact]
    public void SparseVector_Equals()
    {
        var v1 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v2 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v3 = new MilvusSparseVector<float>((int[])[0, 101], (float[])[1.0f, 2.0f]);
        var v4 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 3.0f]);
        var v5 = new MilvusSparseVector<float>((int[])[0], (float[])[1.0f]);

        Assert.True(v1.Equals(v2));
        Assert.True(v1 == v2);
        Assert.False(v1 != v2);
        Assert.True(v1.Equals((object)v2));

        Assert.False(v1.Equals(v3));
        Assert.False(v1 == v3);
        Assert.True(v1 != v3);

        Assert.False(v1.Equals(v4));
        Assert.False(v1.Equals(v5));
        Assert.False(v1.Equals(null));
        Assert.False(v1.Equals("not a vector"));
    }

    [Fact]
    public void SparseVector_GetHashCode()
    {
        var v1 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v2 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v3 = new MilvusSparseVector<float>((int[])[0, 101], (float[])[1.0f, 2.0f]);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }
}
