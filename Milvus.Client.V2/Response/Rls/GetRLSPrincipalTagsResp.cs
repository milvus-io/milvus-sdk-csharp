using System.Text.Json;

using Milvus.Client.V2;

namespace Milvus.Client.V2.Responses.Rls;

/// <summary>
/// The result of getting the tags of a principal for a collection.
/// </summary>
public sealed class GetRLSPrincipalTagsResp
{
    internal GetRLSPrincipalTagsResp(IReadOnlyDictionary<string, object?> tags) => Tags = tags;

    internal static GetRLSPrincipalTagsResp FromGrpc(Grpc.GetRLSPrincipalTagsResponse response)
        => new(DeserializeTags(response.Tags));

    // The server round-trips the tags as an opaque JSON object. Decode into CLR value types (string/long/double/bool,
    // arrays and nested dictionaries) so that, unlike raw JsonElement values, callers can pattern-match directly,
    // e.g. (long)Tags["level"] == 3. Note the request side (SetRLSPrincipalTagsReq) accepts only strings, finite
    // numbers and arrays of them -- bool is rejected -- while the response may still decode booleans.
    private static Dictionary<string, object?> DeserializeTags(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object?>();
        }

        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            // The server contract guarantees a JSON object; a non-object body is precisely the
            // misbehaving-server case and must surface as an error rather than a fabricated empty map,
            // matching the Java and Rust V2 SDKs.
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"RLS principal tags must be a JSON object, but the server returned {document.RootElement.ValueKind}.");
        }

        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value));
    }

    private static object? ConvertJsonValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out long longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            _ => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value))
        };

    /// <summary>
    /// The tags of the principal, preserving integer, float and array values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Tags { get; }
}
