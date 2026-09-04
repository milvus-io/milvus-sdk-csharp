namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The analyzer result for a single analyzed text.
/// </summary>
public sealed class AnalyzerResult
{
    internal AnalyzerResult(IReadOnlyList<AnalyzerToken> tokens) => Tokens = tokens;
    internal static AnalyzerResult FromGrpc(Grpc.AnalyzerResult result)
        => new(result.Tokens.Select(AnalyzerToken.FromGrpc).ToList());

    /// <summary>
    /// The tokens produced by analyzing the text.
    /// </summary>
    public IReadOnlyList<AnalyzerToken> Tokens { get; }
}
