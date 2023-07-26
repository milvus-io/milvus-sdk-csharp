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
        var field = FieldData.CreateFromBytes("byte", new byte[] { 1, 2, 3, 4, 5, 6 }, 2);

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
}
