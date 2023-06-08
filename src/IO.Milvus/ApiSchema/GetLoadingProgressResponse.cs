using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetLoadingProgressResponse
{
    [JsonPropertyName("progress")]
    public long Progress { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}
