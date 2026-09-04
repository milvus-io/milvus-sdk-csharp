using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class NullableFieldDataTests
{
    [Fact]
    public void Nullable_var_char_serializes_valid_data_once()
    {
        var field = new FieldData<string>("name", (IReadOnlyList<string>)(object)new string?[] { "a", null, "c" }, isDynamic: false);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        // valid_data must carry exactly one entry per row (3), not twice as many.
        Assert.Equal(3, grpc.ValidData.Count);
        Assert.Equal(new[] { true, false, true }, grpc.ValidData);
        Assert.Equal(new[] { "a", "c" }, grpc.Scalars.StringData.Data);
    }

    [Fact]
    public void Non_nullable_var_char_serializes_without_valid_data()
    {
        var field = new FieldData<string>("name", new[] { "a", "b", "c" }, isDynamic: false);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Empty(grpc.ValidData);
        Assert.Equal(new[] { "a", "b", "c" }, grpc.Scalars.StringData.Data);
    }

    [Fact]
    public void Nullable_int64_serializes_valid_data_once()
    {
        var field = new FieldData<long?>("count", new long?[] { 1, null, 3 }, isDynamic: false);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(3, grpc.ValidData.Count);
        Assert.Equal(new[] { true, false, true }, grpc.ValidData);
        Assert.Equal(new[] { 1L, 3L }, grpc.Scalars.LongData.Data);
    }
}
