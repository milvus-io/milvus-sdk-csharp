using Google.Protobuf;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// The execution stage at which a <see cref="FunctionChain" /> runs. Numeric values mirror
/// <c>schema.proto FunctionChainStage</c> one-to-one.
/// </summary>
public enum FunctionChainStage
{
    /// <summary>
    /// The stage is unspecified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The chain runs at ingestion time.
    /// </summary>
    Ingestion = 1,

    /// <summary>
    /// The chain runs before the vector search.
    /// </summary>
    PreProcess = 2,

    /// <summary>
    /// The chain reranks L0 candidates.
    /// </summary>
    L0Rerank = 3,

    /// <summary>
    /// The chain reranks L1 candidates.
    /// </summary>
    L1Rerank = 4,

    /// <summary>
    /// The chain reranks L2 candidates.
    /// </summary>
    L2Rerank = 5,

    /// <summary>
    /// The chain runs after all reranking.
    /// </summary>
    PostProcess = 6
}

/// <summary>
/// A typed parameter value of a function-chain expression or operation. Mirrors the Java SDK's
/// <c>FunctionParamValue</c> / pymilvus's <c>_to_param_value</c>: build values with the <see cref="From(object?)" />
/// factories, which map CLR values to the proto oneof (bool / int64 / double / string / bytes / array / object).
/// </summary>
public sealed class FunctionParamValue
{
    private readonly Grpc.FunctionParamValue _grpc;

    private FunctionParamValue(Grpc.FunctionParamValue grpc) => _grpc = grpc;

    internal Grpc.FunctionParamValue ToGrpc() => _grpc;

    /// <summary>
    /// Creates a parameter value from a CLR value. Supported types mirror the Java SDK's
    /// <c>FunctionParamValue.from</c>: <see cref="bool" />, integral numbers (signed and unsigned, the latter
    /// range-checked to int64), <see cref="float" /> / <see cref="double" />, <see cref="string" />,
    /// <see cref="byte" />[], and collections — both already-built <see cref="FunctionParamValue" /> children
    /// and raw <see cref="IEnumerable{T}" /> / <see cref="IReadOnlyDictionary{TKey,TValue}" /> values, which
    /// recurse element-by-element into an array / object.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>The typed parameter value.</returns>
    public static FunctionParamValue From(object? value) => value switch
    {
        null => throw new ArgumentException("A function-chain parameter value cannot be null; pass a non-null value to From, or use FromArray/FromObject.", nameof(value)),
        bool b => new FunctionParamValue(new Grpc.FunctionParamValue { BoolValue = b }),
        byte or sbyte or short or int or long => new FunctionParamValue(new Grpc.FunctionParamValue { Int64Value = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) }),
        uint or ushort => new FunctionParamValue(new Grpc.FunctionParamValue { Int64Value = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) }),
        ulong ul => ul <= long.MaxValue
            ? new FunctionParamValue(new Grpc.FunctionParamValue { Int64Value = (long)ul })
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Function chain integer parameter is out of int64 range."),
        float or double => new FunctionParamValue(new Grpc.FunctionParamValue { DoubleValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture) }),
        string s => new FunctionParamValue(new Grpc.FunctionParamValue { StringValue = s }),
        char c => new FunctionParamValue(new Grpc.FunctionParamValue { StringValue = c.ToString() }),
        byte[] bytes => new FunctionParamValue(new Grpc.FunctionParamValue { BytesValue = ByteString.CopyFrom(bytes) }),
        IEnumerable<FunctionParamValue> values => FromArray(values),
        IReadOnlyDictionary<string, FunctionParamValue> fields => FromObject(fields),
        // Mirror java FunctionParamValue.from / pymilvus _to_param_value: raw collections recurse, so nested
        // list/map values can be passed directly instead of being pre-wrapped with FromArray/FromObject.
        IEnumerable<object?> values => FromArray(values.Select(v => From(v))),
        IReadOnlyDictionary<string, object?> fields => FromObject(
            fields.ToDictionary(pair => pair.Key, pair => From(pair.Value), StringComparer.Ordinal)),
        _ => throw new ArgumentException(
            $"Unsupported function-chain parameter value type '{value.GetType()}'.", nameof(value))
    };

    /// <summary>
    /// Creates an array parameter value.
    /// </summary>
    /// <param name="values">The array elements.</param>
    public static FunctionParamValue FromArray(IEnumerable<FunctionParamValue> values)
    {
        Verify.NotNull(values);
        var grpc = new Grpc.FunctionParamValue { ArrayValue = new Grpc.FunctionParamArray() };
        foreach (FunctionParamValue value in values)
        {
            grpc.ArrayValue.Values.Add(value.ToGrpc());
        }

        return new FunctionParamValue(grpc);
    }

    /// <summary>
    /// Creates an object (map) parameter value.
    /// </summary>
    /// <param name="fields">The named fields.</param>
    public static FunctionParamValue FromObject(IReadOnlyDictionary<string, FunctionParamValue> fields)
    {
        Verify.NotNull(fields);
        var grpc = new Grpc.FunctionParamValue { ObjectValue = new Grpc.FunctionParamObject() };
        foreach (KeyValuePair<string, FunctionParamValue> field in fields)
        {
            grpc.ObjectValue.Fields[field.Key] = field.Value.ToGrpc();
        }

        return new FunctionParamValue(grpc);
    }
}

/// <summary>
/// An argument of a function-chain expression: either a column reference (a collection field, temporary
/// variable, or system name such as <c>$score</c>) or a literal value. Mirrors pymilvus's
/// <c>FunctionChainExprArg</c> / the Java SDK's <c>FunctionChainArg</c>.
/// </summary>
public sealed class FunctionChainArg
{
    /// <summary>
    /// The referenced column name, or <c>null</c> when <see cref="LiteralValue" /> is used.
    /// </summary>
    public string? ColumnName { get; init; }

    /// <summary>
    /// The literal argument value, or <c>null</c> when <see cref="ColumnName" /> is used.
    /// </summary>
    public FunctionParamValue? LiteralValue { get; init; }

    internal Grpc.FunctionChainExprArg ToGrpc()
    {
        // Mirror pymilvus's ColumnRef-vs-literal dispatch: exactly one of the two must be set, and a column
        // reference must be a non-empty name (ColumnRef raises on empty names). Fail fast instead of dereferencing
        // a null literal or silently preferring ColumnName when both are set.
        if (ColumnName is not null && LiteralValue is not null)
        {
            throw new ArgumentException(
                "FunctionChainArg must set exactly one of ColumnName or LiteralValue, not both.", nameof(FunctionChainArg));
        }

        if (ColumnName is not null)
        {
            Verify.NotNullOrWhiteSpace(ColumnName);
            return new Grpc.FunctionChainExprArg { Column = new Grpc.FunctionChainColumnArg { Name = ColumnName } };
        }

        if (LiteralValue is not null)
        {
            return new Grpc.FunctionChainExprArg { Literal = LiteralValue.ToGrpc() };
        }

        throw new ArgumentException(
            "FunctionChainArg must set exactly one of ColumnName or LiteralValue.", nameof(FunctionChainArg));
    }

    /// <summary>
    /// Creates a column-reference argument.
    /// </summary>
    /// <param name="name">The column name, e.g. <c>"$score"</c> or a field name.</param>
    public static FunctionChainArg Column(string name)
    {
        Verify.NotNullOrWhiteSpace(name);
        return new FunctionChainArg { ColumnName = name };
    }

    /// <summary>
    /// Creates a literal argument.
    /// </summary>
    /// <param name="value">The literal value, converted via <see cref="FunctionParamValue.From(object?)" />.</param>
    public static FunctionChainArg Literal(object value)
        => new() { LiteralValue = FunctionParamValue.From(value) };
}

/// <summary>
/// A named function invocation used by a function-chain <c>map</c> operation. Mirrors pymilvus's
/// <c>FunctionChainExpr</c> / the Java SDK's <c>FunctionChainExpr</c>.
/// </summary>
public sealed class FunctionChainExpr
{
    /// <summary>
    /// The function implementation name, e.g. <c>"expr"</c>, <c>"sum"</c> or <c>"rerank_model"</c>.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The ordered expression arguments, interpreted by the function implementation by position.
    /// </summary>
    public IList<FunctionChainArg> Args { get; } = new List<FunctionChainArg>();

    /// <summary>
    /// The expression-level keyword parameters.
    /// </summary>
    public IDictionary<string, FunctionParamValue> Params { get; } = new Dictionary<string, FunctionParamValue>(StringComparer.Ordinal);

    internal Grpc.FunctionChainExpr ToGrpc()
    {
        // Mirror pymilvus/java: an expression without a name is rejected up front.
        Verify.NotNullOrWhiteSpace(Name);

        var grpc = new Grpc.FunctionChainExpr { Name = Name };
        foreach (FunctionChainArg arg in Args)
        {
            grpc.Args.Add(arg.ToGrpc());
        }

        foreach (KeyValuePair<string, FunctionParamValue> parameter in Params)
        {
            grpc.Params[parameter.Key] = parameter.Value.ToGrpc();
        }

        return grpc;
    }
}

/// <summary>
/// A single operation of a <see cref="FunctionChain" /> pipeline. The <see cref="Op" /> name is a
/// server-recognized operation name (e.g. <c>"map"</c>, <c>"sort"</c>, <c>"limit"</c>); the
/// <see cref="Map" /> / <see cref="Sort" /> / <see cref="Limit" /> factories set a valid op, while the raw
/// builder accepts any non-empty string so future server ops remain expressible.
/// </summary>
public sealed class FunctionChainOp
{
    /// <summary>
    /// The <c>map</c> operation, which computes a new output column from an expression.
    /// </summary>
    public const string OpMap = "map";

    /// <summary>
    /// The <c>sort</c> operation, which sorts the results by the given columns.
    /// </summary>
    public const string OpSort = "sort";

    /// <summary>
    /// The <c>limit</c> operation, which keeps only a subset of the results.
    /// </summary>
    public const string OpLimit = "limit";

    /// <summary>
    /// The server-recognized operation name.
    /// </summary>
    public string Op { get; set; } = "";

    /// <summary>
    /// The expression evaluated by this operation, when it is an expression-based operation.
    /// </summary>
    public FunctionChainExpr? Expr { get; set; }

    /// <summary>
    /// The input columns consumed by this operation.
    /// </summary>
    public IList<string> Inputs { get; } = new List<string>();

    /// <summary>
    /// The output columns produced by this operation.
    /// </summary>
    public IList<string> Outputs { get; } = new List<string>();

    /// <summary>
    /// The operation-level parameters keyed by name.
    /// </summary>
    public IDictionary<string, FunctionParamValue> Params { get; } = new Dictionary<string, FunctionParamValue>(StringComparer.Ordinal);

    internal Grpc.FunctionChainOp ToGrpc()
    {
        // Mirror pymilvus/java: an operation without a recognized op name is rejected up front.
        Verify.NotNullOrWhiteSpace(Op);

        var grpc = new Grpc.FunctionChainOp { Op = Op };
        if (Expr is not null)
        {
            grpc.Expr = Expr.ToGrpc();
        }

        grpc.Inputs.AddRange(Inputs);
        grpc.Outputs.AddRange(Outputs);
        foreach (KeyValuePair<string, FunctionParamValue> parameter in Params)
        {
            grpc.Params[parameter.Key] = parameter.Value.ToGrpc();
        }

        return grpc;
    }

    /// <summary>
    /// Creates a <c>map</c> operation computing the <paramref name="output" /> column from <paramref name="expr" />.
    /// </summary>
    public static FunctionChainOp Map(string output, FunctionChainExpr expr)
    {
        Verify.NotNullOrWhiteSpace(output);
        Verify.NotNull(expr);
        return new FunctionChainOp
        {
            Op = OpMap,
            Expr = expr,
            Outputs = { output }
        };
    }

    /// <summary>
    /// Creates a <c>sort</c> operation sorting by the given column.
    /// </summary>
    /// <param name="by">The column to sort by.</param>
    /// <param name="desc"><c>true</c> to sort in descending order.</param>
    /// <param name="tieBreakCol">An optional column used to break ties.</param>
    public static FunctionChainOp Sort(string by, bool desc, string? tieBreakCol = null)
    {
        Verify.NotNullOrWhiteSpace(by);
        var op = new FunctionChainOp { Op = OpSort, Inputs = { by } };
        op.Params["column"] = FunctionParamValue.From(by);
        op.Params["desc"] = FunctionParamValue.From(desc);
        if (!string.IsNullOrEmpty(tieBreakCol))
        {
            op.Params["tie_break_col"] = FunctionParamValue.From(tieBreakCol!);
            op.Inputs.Add(tieBreakCol!);
        }

        return op;
    }

    /// <summary>
    /// Creates a <c>limit</c> operation keeping only <paramref name="limit" /> results, skipping the given
    /// number of leading results.
    /// </summary>
    public static FunctionChainOp Limit(int limit, int offset = 0)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Function chain limit must be a positive integer.");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Function chain offset must be non-negative.");
        }

        var op = new FunctionChainOp { Op = OpLimit };
        op.Params["limit"] = FunctionParamValue.From((long)limit);
        op.Params["offset"] = FunctionParamValue.From((long)offset);
        return op;
    }
}

/// <summary>
/// An ordered rerank/refine plan applied to search results, mirroring pymilvus's / the Java SDK's
/// <c>FunctionChain</c>: a chain composes <see cref="FunctionChainOp.Map" />, <see cref="FunctionChainOp.Sort" />
/// and <see cref="FunctionChainOp.Limit" /> operations and serializes to the gRPC <c>FunctionChain</c> message.
/// </summary>
/// <example>
/// <code>
/// var chain = new FunctionChain
/// {
///     Stage = FunctionChainStage.L2Rerank,
///     Name = "fresh_popular_rerank",
///     Ops =
///     {
///         FunctionChainOp.Map("$score", new FunctionChainExpr
///         {
///             Name = "num_combine",
///             Args = { FunctionChainArg.Column("$score"), FunctionChainArg.Column("freshness") },
///             Params = { ["mode"] = FunctionParamValue.From("weighted") }
///         }),
///         FunctionChainOp.Sort("$score", desc: true, tieBreakCol: "$id"),
///         FunctionChainOp.Limit(10)
///     }
/// };
/// </code>
/// </example>
public sealed class FunctionChain
{
    /// <summary>
    /// The execution stage where this function chain runs.
    /// </summary>
    public FunctionChainStage Stage { get; set; }

    /// <summary>
    /// An optional SDK/debug name of this function chain.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The ordered operations of this function chain, executed by the server in this order.
    /// </summary>
    public IList<FunctionChainOp> Ops { get; } = new List<FunctionChainOp>();

    internal Grpc.FunctionChain ToGrpc()
    {
        // Mirror java's "stage is required" (and pymilvus's valid-stage check): a chain whose stage was left at
        // the default Unspecified has no meaningful execution stage and must not be sent to the server.
        if (Stage == FunctionChainStage.Unspecified)
        {
            throw new ArgumentException(
                "FunctionChain.Stage must be set to a non-unspecified stage (e.g. L2Rerank).", nameof(Stage));
        }

        var grpc = new Grpc.FunctionChain { Name = Name, Stage = (Grpc.FunctionChainStage)(int)Stage };
        foreach (FunctionChainOp op in Ops)
        {
            grpc.Ops.Add(op.ToGrpc());
        }

        return grpc;
    }
}
