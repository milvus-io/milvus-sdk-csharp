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

    [Fact]
    public void Var_char_honors_explicit_valid_data()
    {
        // A caller-flagged invalid row carries a placeholder value; it must still be dropped via valid_data.
        var field = new FieldData<string>("name", new[] { "a", "should-be-dropped", "c" }, isDynamic: false)
        {
            ValidData = new[] { true, false, true }
        };

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(3, grpc.ValidData.Count);
        Assert.Equal(new[] { true, false, true }, grpc.ValidData);
        Assert.Equal(new[] { "a", "c" }, grpc.Scalars.StringData.Data);
    }

    [Fact]
    public void Json_honors_explicit_valid_data()
    {
        var field = new FieldData<string>("meta", new[] { "{\"k\":1}", "{\"k\":2}" }, DataType.Json, isDynamic: false)
        {
            ValidData = new[] { true, false }
        };

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(2, grpc.ValidData.Count);
        Assert.Equal(new[] { true, false }, grpc.ValidData);
        Assert.Equal(new[] { "{\"k\":1}" }, grpc.Scalars.JsonData.Data.Select(b => b.ToStringUtf8()));
    }

    [Fact]
    public void Geometry_serializes_wkt_into_dedicated_slot()
    {
        var field = new FieldData<string>("geo", new[] { "POINT(1 1)", "POLYGON((0 0, 0 1, 1 0, 0 0))" }, DataType.Geometry);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        // Geometry rows must travel in the geometry_wkt_data slot, not string_data (the proxy rejects the
        // latter for geometry fields), mirroring the Java SDK.
        Assert.Equal(Grpc.DataType.Geometry, grpc.Type);
        // Geometry must use the dedicated geometry_wkt_data slot, not string_data.
        Assert.Equal(Grpc.ScalarField.DataOneofCase.GeometryWktData, grpc.Scalars.DataCase);
        Assert.Equal(
            new[] { "POINT(1 1)", "POLYGON((0 0, 0 1, 1 0, 0 0))" },
            grpc.Scalars.GeometryWktData.Data);
    }

    [Fact]
    public void Geometry_serializes_null_rows_via_valid_data()
    {
        var field = new FieldData<string>("geo", (IReadOnlyList<string>)(object)new string?[] { "POINT(1 1)", null, "POINT(2 2)" }, DataType.Geometry);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Equal(new[] { true, false, true }, grpc.ValidData);
        Assert.Equal(new[] { "POINT(1 1)", "POINT(2 2)" }, grpc.Scalars.GeometryWktData.Data);
    }

    [Fact]
    public void Non_nullable_int_honors_explicit_valid_data()
    {
        var field = new FieldData<int>("count", new[] { 1, 2, 3 }, isDynamic: false)
        {
            ValidData = new[] { true, false, true }
        };

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        // A caller-flagged invalid row on a non-nullable column is not packed and emits a valid_data=false flag.
        Assert.Equal(new[] { true, false, true }, grpc.ValidData);
        Assert.Equal(new[] { 1, 3 }, grpc.Scalars.IntData.Data);
    }

    [Fact]
    public void Non_nullable_long_without_valid_data_packs_all_rows()
    {
        var field = new FieldData<long>("count", new[] { 1L, 2L }, isDynamic: false);

        Grpc.FieldData grpc = field.ToGrpcFieldData();

        Assert.Empty(grpc.ValidData);
        Assert.Equal(new[] { 1L, 2L }, grpc.Scalars.LongData.Data);
    }
}
