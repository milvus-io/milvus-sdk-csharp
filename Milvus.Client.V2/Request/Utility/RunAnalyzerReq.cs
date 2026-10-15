using System.Text.Json;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to run a text analyzer on the given strings, returning the analyzed tokens.
/// </summary>
public sealed class RunAnalyzerReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The analyzer configuration as a JSON object, e.g. <c>new Dictionary&lt;string, object&gt; { ["type"] = "english" }</c>.
    /// Serialized to JSON before sending. Mutually exclusive with <see cref="CollectionName" />/<see cref="FieldName" />.
    /// </summary>
    public IReadOnlyDictionary<string, object>? AnalyzerParams { get; set; }

    /// <summary>
    /// The texts to analyze.
    /// </summary>
    public IReadOnlyList<string> Texts { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to return the detailed token offsets and positions.
    /// </summary>
    public bool WithDetail { get; set; }

    /// <summary>
    /// Whether to return the token hashes.
    /// </summary>
    public bool WithHash { get; set; }

    /// <summary>
    /// The collection whose analyzer configuration is used. Required when <see cref="FieldName" /> is set.
    /// </summary>
    public string? CollectionName { get; set; }

    /// <summary>
    /// The field whose analyzer configuration is used.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The names of predefined analyzers to apply, when not using <see cref="AnalyzerParams" />.
    /// </summary>
    public IReadOnlyList<string>? AnalyzerNames { get; set; }

    internal Grpc.RunAnalyzerRequest ToGrpcRunAnalyzerRequest()
    {
        Verify.NotNullOrEmpty(Texts);

        // The server treats analyzer_params and (collection, field) as mutually exclusive
        // (validateRunAnalyzer rejects any request where both are set); validate the combination up front so
        // callers fail fast instead of hitting a server-side rejection.
        bool usesFieldAnalyzer = CollectionName is not null || FieldName is not null;
        if (AnalyzerParams is { Count: > 0 } && usesFieldAnalyzer)
        {
            throw new ArgumentException(
                "AnalyzerParams is mutually exclusive with CollectionName/FieldName; use either an ad hoc analyzer or a field's configured analyzer.",
                nameof(AnalyzerParams));
        }

        // A field/named analyzer without a collection would be silently dropped server-side (the proxy routes
        // to the ad-hoc path and ignores field_name/analyzer_names); require the collection. The ad-hoc mode
        // (AnalyzerParams set, no field) is unaffected.
        if (AnalyzerParams is not { Count: > 0 }
            && string.IsNullOrWhiteSpace(CollectionName)
            && (FieldName is not null || AnalyzerNames is { Count: > 0 }))
        {
            throw new ArgumentException(
                "CollectionName is required when FieldName or AnalyzerNames is set.",
                nameof(CollectionName));
        }

        var request = new Grpc.RunAnalyzerRequest
        {
            WithDetail = WithDetail,
            WithHash = WithHash,
            CollectionName = CollectionName ?? "",
            FieldName = FieldName ?? ""
        };

        // Only serialize analyzer_params when one was provided; an empty dictionary must not become "{}" on
        // the wire, which the server would treat as an ad hoc analyzer alongside the field analyzer.
        if (AnalyzerParams is { Count: > 0 })
        {
            request.AnalyzerParams = JsonSerializer.Serialize(AnalyzerParams);
        }

        foreach (string text in Texts)
        {
            request.Placeholder.Add(ByteString.CopyFromUtf8(text));
        }
        if (AnalyzerNames is not null)
        {
            request.AnalyzerNames.AddRange(AnalyzerNames);
        }
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
