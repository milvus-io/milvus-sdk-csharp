using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class FieldDataToGrpcTests
{
    [Fact]
    public void ToGrpc_encodes_int_widths_into_int_slot()
    {
        Assert.Equal(new[] { 1, -2 }, ToGrpc(FieldData.Create("f", new sbyte[] { 1, -2 })).Scalars.IntData.Data.ToArray());
        Assert.Equal(new[] { 3, -4 }, ToGrpc(FieldData.Create("f", new short[] { 3, -4 })).Scalars.IntData.Data.ToArray());
        Assert.Equal(new[] { 5, -6 }, ToGrpc(FieldData.Create("f", new[] { 5, -6 })).Scalars.IntData.Data.ToArray());
    }

    [Fact]
    public void ToGrpc_encodes_scalar_slots()
    {
        Assert.Equal(new[] { 1L, 2L }, ToGrpc(FieldData.Create("f", new long[] { 1, 2 })).Scalars.LongData.Data.ToArray());
        Assert.Equal(new[] { true, false }, ToGrpc(FieldData.Create("f", new[] { true, false })).Scalars.BoolData.Data.ToArray());
        Assert.Equal(new[] { 1.5f, 2.5f }, ToGrpc(FieldData.Create("f", new[] { 1.5f, 2.5f })).Scalars.FloatData.Data.ToArray());
        Assert.Equal(new[] { 1.5, 2.5 }, ToGrpc(FieldData.Create("f", new[] { 1.5, 2.5 })).Scalars.DoubleData.Data.ToArray());
        Assert.Equal(new[] { "a", "b" }, ToGrpc(FieldData.Create("f", new[] { "a", "b" })).Scalars.StringData.Data.ToArray());
    }

    [Fact]
    public void ToGrpc_encodes_json_strings_into_json_slot()
    {
        FieldData field = FieldData.CreateJson("f", new[] { "{\"a\":1}", "{\"b\":2}" });

        Grpc.FieldData grpc = field.ToGrpcFieldData();
        Assert.Equal(2, grpc.Scalars.JsonData.Data.Count);
        Assert.Equal("{\"a\":1}", grpc.Scalars.JsonData.Data[0].ToStringUtf8());
    }

    [Fact]
    public void ToGrpc_emits_valid_data_for_nullable_rows()
    {
        // A nullable int column with a null row must flag it via valid_data rather than coercing to 0.
        FieldData<long?> field = FieldData.Create("f", new long?[] { 1, null, 3 });

        Grpc.FieldData grpc = field.ToGrpcFieldData();
        Assert.Equal(new[] { true, false, true }, grpc.ValidData.ToArray());
        Assert.Equal(new[] { 1L, 3L }, grpc.Scalars.LongData.Data.ToArray());
    }

    private static Grpc.FieldData ToGrpc(FieldData field) => field.ToGrpcFieldData();
}
