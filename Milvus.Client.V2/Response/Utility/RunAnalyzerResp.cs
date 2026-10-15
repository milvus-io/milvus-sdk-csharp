namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <c>RunAnalyzer</c> operation, containing the analyzed tokens for each input text.
/// </summary>
public sealed class RunAnalyzerResp
{
    internal RunAnalyzerResp(IReadOnlyList<AnalyzerResult> results) => Results = results;
    internal static RunAnalyzerResp FromGrpc(Grpc.RunAnalyzerResponse response)
        => new(response.Results.Select(AnalyzerResult.FromGrpc).ToList());

    /// <summary>
    /// The analysis results, one per analyzed text.
    /// </summary>
    public IReadOnlyList<AnalyzerResult> Results { get; }
}
