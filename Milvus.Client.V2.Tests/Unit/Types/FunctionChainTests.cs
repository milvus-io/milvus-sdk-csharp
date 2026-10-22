using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class FunctionChainTests
{
    [Fact]
    public void FunctionParamValue_accepts_signed_integrals()
    {
        Assert.Equal(5L, FunctionParamValue.From((byte)5).ToGrpc().Int64Value);
        Assert.Equal(-3L, FunctionParamValue.From((sbyte)-3).ToGrpc().Int64Value);
        Assert.Equal(42L, FunctionParamValue.From(42).ToGrpc().Int64Value);
    }

    [Fact]
    public void FunctionParamValue_accepts_unsigned_integrals_in_int64_range()
    {
        Assert.Equal(5L, FunctionParamValue.From((uint)5).ToGrpc().Int64Value);
        Assert.Equal(3L, FunctionParamValue.From((ushort)3).ToGrpc().Int64Value);
        Assert.Equal(10L, FunctionParamValue.From((ulong)10).ToGrpc().Int64Value);
        Assert.Equal(long.MaxValue, FunctionParamValue.From((ulong)long.MaxValue).ToGrpc().Int64Value);
    }

    [Fact]
    public void FunctionParamValue_rejects_ulong_out_of_int64_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FunctionParamValue.From(ulong.MaxValue));
    }

    [Fact]
    public void FunctionChainArg_maps_column_and_literal()
    {
        Grpc.FunctionChainExprArg column = FunctionChainArg.Column("$score").ToGrpc();
        Assert.Equal("$score", column.Column.Name);

        Grpc.FunctionChainExprArg literal = FunctionChainArg.Literal(42).ToGrpc();
        Assert.Equal(42L, literal.Literal.Int64Value);
    }

    [Fact]
    public void FunctionChainArg_requires_exactly_one_of_column_or_literal()
    {
        Assert.Throws<ArgumentException>(() => new FunctionChainArg().ToGrpc());
        Assert.Throws<ArgumentException>(() => new FunctionChainArg { ColumnName = "x", LiteralValue = FunctionParamValue.From(1) }.ToGrpc());
    }

    [Fact]
    public void FunctionChainArg_rejects_empty_column_name()
    {
        Assert.Throws<ArgumentException>(() => new FunctionChainArg { ColumnName = " " }.ToGrpc());
    }

    [Fact]
    public void FunctionParamValue_recurses_raw_collections()
    {
        Grpc.FunctionParamValue array = FunctionParamValue.From(new List<object?> { 1, "x", 1.5 }).ToGrpc();
        Assert.Equal(3, array.ArrayValue.Values.Count);
        Assert.Equal(1L, array.ArrayValue.Values[0].Int64Value);
        Assert.Equal("x", array.ArrayValue.Values[1].StringValue);
        Assert.Equal(1.5, array.ArrayValue.Values[2].DoubleValue);

        Grpc.FunctionParamValue obj = FunctionParamValue.From(
            new Dictionary<string, object?> { ["mode"] = "weighted", ["k"] = 3 }).ToGrpc();
        Assert.Equal("weighted", obj.ObjectValue.Fields["mode"].StringValue);
        Assert.Equal(3L, obj.ObjectValue.Fields["k"].Int64Value);
    }

    [Fact]
    public void FunctionChainOp_requires_non_empty_op_name()
    {
        Assert.Throws<ArgumentException>(() => new FunctionChainOp().ToGrpc());
    }

    [Fact]
    public void FunctionChainExpr_requires_non_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new FunctionChainExpr().ToGrpc());
    }

    [Fact]
    public void FunctionChain_requires_a_stage()
    {
        var chain = new FunctionChain { Ops = { FunctionChainOp.Limit(10) } };
        Assert.Throws<ArgumentException>(() => chain.ToGrpc());
    }

    [Fact]
    public void FunctionChain_maps_stage_name_and_ops()
    {
        var chain = new FunctionChain
        {
            Stage = FunctionChainStage.L2Rerank,
            Name = "rerank",
            Ops = { FunctionChainOp.Limit(10) }
        };

        Grpc.FunctionChain grpc = chain.ToGrpc();

        Assert.Equal(Grpc.FunctionChainStage.L2Rerank, grpc.Stage);
        Assert.Equal("rerank", grpc.Name);
        Assert.Equal("limit", Assert.Single(grpc.Ops).Op);
    }
}
