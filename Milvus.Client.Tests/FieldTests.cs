using FluentAssertions;
using Xunit;

namespace Milvus.Client.Tests;

public class FieldTests
{
    [Fact]
    public void ToStringTest()
    {
        var field = FieldData.Create("Id", new[] { 1, 2, 3 });
        field.RowCount.Should().Be(3);
        field.ToString().Should().Be("Field: {FieldName: Id, DataType: Int32, Data: 3, RowCount: 3}");
    }

    [Fact]
    public void CreateTest()
    {
        var field = FieldData.Create("Id", new[] { 1, 2, 3 });
        field.Should().NotBeNull();
        field.FieldName.Should().Be("Id");
        field.DataType.Should().Be(MilvusDataType.Int32);
        field.RowCount.Should().Be(3);
        field.Data.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void CreateVarCharTest()
    {
        var field = FieldData.CreateVarChar("id", new[] { "fsj", "fsd" });

        field.Should().NotBeNull();
        field.FieldName.Should().Be("id");
        field.DataType.Should().Be(MilvusDataType.VarChar);
    }

    [Fact]
    public void CreateFromBytesTest()
    {
        var field = FieldData.CreateFromBytes("byte", new byte[] { 1, 2, 3, 4, 5, 6 }, 2);

        field.Should().NotBeNull();
        field.RowCount.Should().Be(3);
        field.Data.Count.Should().Be(3);
        field.DataType.Should().Be(MilvusDataType.BinaryVector);
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

        field.Should().NotBeNull();
        field.RowCount.Should().Be(2);
        field.Data.Count.Should().Be(2);
        field.DataType.Should().Be(MilvusDataType.BinaryVector);
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

        field.Should().NotBeNull();
        field.RowCount.Should().Be(2);
        field.Data.Count.Should().Be(2);
        field.DataType.Should().Be(MilvusDataType.FloatVector);
    }
}
