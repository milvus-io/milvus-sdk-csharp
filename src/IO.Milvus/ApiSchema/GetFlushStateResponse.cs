using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetFlushStateResponse
{
    [JsonPropertyName("flushed")]
    public bool Flushed { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}