using Xunit;
using FluentAssertions;

namespace IO.Milvus.Tests;

public class FieldTests
{
    [Fact()]
    public void ToStringTest()
    {
        var field = Field.Create<int>("Id", new[] { 1, 2, 3 });
        field.RowCount.Should().Be(3);
        field.ToString().Should().Be("Field: {FieldName: Id, DataType: Int32, Data: 3, RowCount: 3}");
    }

    [Fact()]
    public void ToGrpcFieldDataTest()
    {
        var field = Field.Create<int>("Id", new[] { 1, 2, 3 });
        var grpcField = field.ToGrpcFieldData();
        grpcField.Should().NotBeNull();
        grpcField.FieldName.Should().Be("Id");
        grpcField.Type.Should().Be(Grpc.DataType.Int32);
        grpcField.Scalars.IntData.Data.Count.Should().Be(3);
    }

    [Fact()]
    public void FromGrpcFieldDataTest()
    {
        var grpcField = new Grpc.FieldData()
        {
            FieldName = "Id",
            Type = Grpc.DataType.Int32,
            Scalars = new Grpc.ScalarField()
            {
                IntData = new Grpc.IntArray()
            }
        };

        grpcField.Scalars.IntData.Data.Add(new[] { 1, 2, 3 });

        var field = Field.FromGrpcFieldData(grpcField) as Field<int>;
        field.Should().NotBeNull();
        field?.FieldName.Should().Be("Id");
        field?.DataType.Should().Be(MilvusDataType.Int32);
        field?.RowCount.Should().Be(3);
        field?.Data.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact()]
    public void CreateTest()
    {
        var field = Field.Create<int>("Id", new[] { 1, 2, 3 });
        field.Should().NotBeNull();
        field.FieldName.Should().Be("Id");
        field.DataType.Should().Be(MilvusDataType.Int32);
        field.RowCount.Should().Be(3);
        field.Data.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact()]
    public void CreateVarCharTest()
    {
        var field = Field.CreateVarChar("id", new[] { "fsj", "fsd" });

        field.Should().NotBeNull();
        field.FieldName.Should().Be("id");
        field.DataType.Should().Be(MilvusDataType.VarChar);
    }

    [Fact()]
    public void CreateFromBytesTest()
    {
        var field = Field.CreateFromBytes("byte", new byte[] { 1, 2, 3, 4, 5, 6 }, 2);

        field.Should().NotBeNull();
        field.RowCount.Should().Be(3);
        field.Data.Count.Should().Be(3);
        field.DataType.Should().Be(MilvusDataType.BinaryVector);
    }

    [Fact()]
    public void CreateBinaryVectorsTest()
    {
        var field = Field.CreateBinaryVectors(
            "byte"
            , new List<byte[]>() {
                new byte[] { 1,2},
                new byte[] { 3,4}
            });

        field.Should().NotBeNull();
        field.RowCount.Should().Be(2);
        field.Data.Count.Should().Be(2);
        field.DataType.Should().Be(MilvusDataType.BinaryVector);
    }

    [Fact()]
    public void CreateFloatVectorTest()
    {
        var field = Field.CreateFloatVector(
            "vector",
            new List<List<float>>() {
                new List<float>() { 1, 2 },
                new List<float>() { 3, 4 }
            });

        field.Should().NotBeNull();
        field.RowCount.Should().Be(2);
        field.Data.Count.Should().Be(2);
        field.DataType.Should().Be(MilvusDataType.FloatVector);
    }
}