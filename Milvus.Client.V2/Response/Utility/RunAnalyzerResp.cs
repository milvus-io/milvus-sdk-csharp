#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class RunAnalyzerResp
{
    internal RunAnalyzerResp(IReadOnlyList<AnalyzerResult> results) => Results = results;
    internal static RunAnalyzerResp FromGrpc(Grpc.RunAnalyzerResponse response)
        => new(response.Results.Select(AnalyzerResult.FromGrpc).ToList());
    public IReadOnlyList<AnalyzerResult> Results { get; }
}
