using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A structured rerank configuration used by <c>search</c>/<c>hybridSearch</c>, mapped to the proto
/// <c>schema.FunctionScore</c> (functions + params). Mirrors the Java SDK's <c>FunctionScore</c> and the C++
/// SDK's <c>FunctionScore</c>. The functions carry the rerank strategy (boost/decay/model) and its parameters;
/// the top-level params are function-score-level options.
/// </summary>
public sealed class FunctionScore
{
    /// <summary>
    /// The rerank functions, each a <see cref="FunctionSchema" /> of type <see cref="FunctionType.Rerank" />.
    /// </summary>
    public IList<FunctionSchema> Functions { get; } = new List<FunctionSchema>();

    /// <summary>
    /// Function-score-level parameters (sent as the proto <c>FunctionScore.params</c> key-value pairs).
    /// </summary>
    public IDictionary<string, string> Params { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Converts this <see cref="FunctionScore" /> to its wire representation.
    /// </summary>
    internal Grpc.FunctionScore ToGrpcFunctionScore()
    {
        var result = new Grpc.FunctionScore();
        foreach (FunctionSchema function in Functions)
        {
            result.Functions.Add(function.ToGrpcFunctionSchema());
        }

        foreach (KeyValuePair<string, string> parameter in Params)
        {
            result.Params.Add(new Grpc.KeyValuePair { Key = parameter.Key, Value = parameter.Value });
        }

        return result;
    }
}
