using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class ArrayFieldDataTests
{
    [Fact]
    public void Bool_element_serializes_to_bool_arrays()
    {
        var field = new ArrayFieldData<bool>("flags",
            new IReadOnlyList<bool>?[] { new[] { true, false }, new[] { true } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal("flags", grpc.FieldName);
        Assert.Equal(Grpc.DataType.Array, grpc.Type);
        Assert.False(grpc.IsDynamic);
        Assert.Equal(DataType.Bool, field.ElementType);
        Assert.Equal(Grpc.DataType.Bool, grpc.Scalars.ArrayData.ElementType);

        Assert.Equal(2, grpc.Scalars.ArrayData.Data.Count);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.BoolData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { true, false }, grpc.Scalars.ArrayData.Data[0].BoolData.Data);
        Assert.Equal(new[] { true }, grpc.Scalars.ArrayData.Data[1].BoolData.Data);
    }

    [Fact]
    public void Int8_element_serializes_to_int_arrays()
    {
        var field = new ArrayFieldData<sbyte>("bytes",
            new IReadOnlyList<sbyte>?[] { new[] { (sbyte)1, (sbyte)-2 } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.Int8, field.ElementType);
        Assert.Equal(Grpc.DataType.Int8, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.IntData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { 1, -2 }, grpc.Scalars.ArrayData.Data[0].IntData.Data);
    }

    [Fact]
    public void Int16_element_serializes_to_int_arrays()
    {
        var field = new ArrayFieldData<short>("shorts",
            new IReadOnlyList<short>?[] { new short[] { 300, -5 } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.Int16, field.ElementType);
        Assert.Equal(Grpc.DataType.Int16, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.IntData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { 300, -5 }, grpc.Scalars.ArrayData.Data[0].IntData.Data);
    }

    [Fact]
    public void Int32_element_serializes_to_int_arrays()
    {
        var field = new ArrayFieldData<int>("ints",
            new IReadOnlyList<int>?[] { new[] { 1, 2, 3 } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.Int32, field.ElementType);
        Assert.Equal(Grpc.DataType.Int32, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.IntData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { 1, 2, 3 }, grpc.Scalars.ArrayData.Data[0].IntData.Data);
    }

    [Fact]
    public void Int64_element_serializes_to_long_arrays()
    {
        var field = new ArrayFieldData<long>("longs",
            new IReadOnlyList<long>?[] { new long[] { 1, long.MaxValue } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.Int64, field.ElementType);
        Assert.Equal(Grpc.DataType.Int64, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.LongData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new long[] { 1, long.MaxValue }, grpc.Scalars.ArrayData.Data[0].LongData.Data);
    }

    [Fact]
    public void Float_element_serializes_to_float_arrays()
    {
        var field = new ArrayFieldData<float>("floats",
            new IReadOnlyList<float>?[] { new[] { 1.5f, -2.5f } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.Float, field.ElementType);
        Assert.Equal(Grpc.DataType.Float, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.FloatData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { 1.5f, -2.5f }, grpc.Scalars.ArrayData.Data[0].FloatData.Data);
    }

    [Fact]
    public void Double_element_serializes_to_double_arrays()
    {
        var field = new ArrayFieldData<double>("doubles",
            new IReadOnlyList<double>?[] { new[] { 1.5, 2.5 } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.Double, field.ElementType);
        Assert.Equal(Grpc.DataType.Double, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.DoubleData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { 1.5, 2.5 }, grpc.Scalars.ArrayData.Data[0].DoubleData.Data);
    }

    [Fact]
    public void VarChar_element_serializes_to_string_arrays()
    {
        var field = new ArrayFieldData<string>("tags",
            new IReadOnlyList<string>?[] { new[] { "a", "b" }, new[] { "c" } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(DataType.VarChar, field.ElementType);
        Assert.Equal(Grpc.DataType.VarChar, grpc.Scalars.ArrayData.ElementType);
        Assert.Equal(Grpc.ScalarField.DataOneofCase.StringData, grpc.Scalars.ArrayData.Data[0].DataCase);
        Assert.Equal(new[] { "a", "b" }, grpc.Scalars.ArrayData.Data[0].StringData.Data);
        Assert.Equal(new[] { "c" }, grpc.Scalars.ArrayData.Data[1].StringData.Data);
    }

    [Fact]
    public void Null_rows_are_marked_invalid_and_skipped()
    {
        var field = new ArrayFieldData<string>("tags",
            new IReadOnlyList<string>?[] { new[] { "a", "b" }, null, new[] { "c" } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        // One valid_data entry per row: non-null rows are true, the null row is false.
        Assert.Equal(new[] { true, false, true }, grpc.ValidData);
        Grpc.ArrayArray array = grpc.Scalars.ArrayData;
        Assert.Equal(2, array.Data.Count);
        Assert.Equal(new[] { "a", "b" }, array.Data[0].StringData.Data);
        Assert.Equal(new[] { "c" }, array.Data[1].StringData.Data);
    }

    [Fact]
    public void Without_null_rows_valid_data_is_empty()
    {
        var field = new ArrayFieldData<int>("ints",
            new IReadOnlyList<int>?[] { new[] { 1 }, new[] { 2 } });

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Empty(grpc.ValidData);
        Assert.Equal(2, grpc.Scalars.ArrayData.Data.Count);
    }

    [Fact]
    public void IsDynamic_is_preserved()
    {
        var field = new ArrayFieldData<int>("ids", new IReadOnlyList<int>?[] { new[] { 1 } }, isDynamic: true);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.True(grpc.IsDynamic);
        Assert.True(field.IsDynamic);
    }

    [Fact]
    public void Base_field_properties_are_exposed()
    {
        var field = new ArrayFieldData<long>("ids",
            new IReadOnlyList<long>?[] { new long[] { 1, 2 }, null });

        Assert.Equal("ids", field.FieldName);
        Assert.Equal(DataType.Array, field.DataType);
        Assert.False(field.IsDynamic);
        Assert.Equal(2, field.RowCount);
        Assert.Equal(DataType.Int64, field.ElementType);
    }

    [Fact]
    public void Unsupported_element_type_throws()
    {
        var field = new ArrayFieldData<byte>("ids", new IReadOnlyList<byte>?[] { new byte[] { 1 } });

        Assert.Throws<NotSupportedException>(() => field.ElementType);
        Assert.Throws<NotSupportedException>(() => field.ToGrpcFieldData());
    }
}
