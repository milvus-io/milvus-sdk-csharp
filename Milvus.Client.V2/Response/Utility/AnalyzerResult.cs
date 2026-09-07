#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class AnalyzerResult
{
    internal AnalyzerResult(IReadOnlyList<AnalyzerToken> tokens) => Tokens = tokens;
    internal static AnalyzerResult FromGrpc(Grpc.AnalyzerResult result)
        => new(result.Tokens.Select(AnalyzerToken.FromGrpc).ToList());
    public IReadOnlyList<AnalyzerToken> Tokens { get; }
}
