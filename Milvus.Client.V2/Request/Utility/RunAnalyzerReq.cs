#pragma warning disable CS1591 // Missing XML docs
using System.Text.Json;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;
public sealed class RunAnalyzerReq
{
    /// <summary>
    /// The analyzer configuration as a JSON object, e.g. <c>new Dictionary&lt;string, object&gt; { ["type"] = "english" }</c>.
    /// Serialized to JSON before sending.
    /// </summary>
    public IReadOnlyDictionary<string, object> AnalyzerParams { get; set; } =
        new Dictionary<string, object>();
    public IReadOnlyList<string> Texts { get; set; } = Array.Empty<string>();
    public bool WithDetail { get; set; }
    public bool WithHash { get; set; }
    public string? CollectionName { get; set; }
    public string? FieldName { get; set; }
    public IReadOnlyList<string>? AnalyzerNames { get; set; }
    internal Grpc.RunAnalyzerRequest ToGrpcRunAnalyzerRequest()
    {
        Verify.NotNull(AnalyzerParams);
        Verify.NotNullOrEmpty(Texts);
        var request = new Grpc.RunAnalyzerRequest
        {
            AnalyzerParams = JsonSerializer.Serialize(AnalyzerParams),
            WithDetail = WithDetail,
            WithHash = WithHash,
            CollectionName = CollectionName ?? "",
            FieldName = FieldName ?? ""
        };
        foreach (string text in Texts)
        {
            request.Placeholder.Add(ByteString.CopyFromUtf8(text));
        }
        if (AnalyzerNames is not null)
        {
            request.AnalyzerNames.AddRange(AnalyzerNames);
        }
        return request;
    }
}
