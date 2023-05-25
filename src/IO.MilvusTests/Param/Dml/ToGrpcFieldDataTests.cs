using FluentAssertions;
using IO.Milvus.Param.Dml;
using Xunit;

namespace IO.MilvusTests.Param.Dml;

public sealed class ToGrpcFieldDataTests
{
    [Fact]
    public void StringTest()
    {
        var stringField = new Field<string>
        {
            Datas = new List<string>()
            {
                "aaaa", "bbbb",
            },
            FieldName = "test"
        };

        var grpcField = stringField.ToGrpcFieldData();

        grpcField.Should().NotBeNull();
        grpcField.Scalars.StringData.Data.Count.Should().Be(2);
    }

    [Fact]
    public void BoolTest()
    {
        var boolField = new Field<bool>
        {
            Datas = new List<bool>()
            {
                true, true,
            },
            FieldName = "test"
        };

        var grpcField = boolField.ToGrpcFieldData();

        grpcField.Should().NotBeNull();
        grpcField.Scalars.BoolData.Data.Count.Should().Be(2);
    }

    [Fact]
    public void IntTest()
    {
        var intField = new Field<int>
        {
            Datas = new List<int>()
            {
                1, 2,
            },
            FieldName = "test"
        };

        var grpcField = intField.ToGrpcFieldData();

        intField.Should().NotBeNull();
        grpcField.Scalars.IntData.Data.Count.Should().Be(2);
    }

    [Fact]
    public void Int16Test()
    {
        var intField = new Field<Int16>
        {
            Datas = new List<Int16>()
            {
                1, 2,
            },
            FieldName = "test"
        };

        var grpcField = intField.ToGrpcFieldData();

        grpcField.Should().NotBeNull();
        grpcField.Scalars.IntData.Data.Count.Should().Be(2);
    }

    [Fact]
    public void Int32Test()
    {
        var intField = new Field<Int32>
        {
            Datas = new List<Int32>()
            {
                1, 2,
            },
            FieldName = "test"
        };

        var grpcField = intField.ToGrpcFieldData();
        grpcField.Should().NotBeNull();
        grpcField.Scalars.IntData.Data.Count.Should().Be(2);
    }

    [Fact]
    public void Int64Test()
    {
        var intField = new Field<Int64>
        {
            Datas = new List<Int64>()
            {
                1, 2,
            },
            FieldName = "test"
        };

        var grpcField = intField.ToGrpcFieldData();

        grpcField.Should().NotBeNull();
        grpcField.Scalars.LongData.Data.Count.Should().Be(2);
    }

    [Fact]
    public void LongTest()
    {
        var intField = new Field<long>
        {
            Datas = new List<long>()
            {
                1, 2,
            },
            FieldName = "test"
        };

        var grpcField = intField.ToGrpcFieldData();

        grpcField.Should().NotBeNull();
        grpcField.Scalars.LongData.Data.Count.Should().Be(2);
    }
}