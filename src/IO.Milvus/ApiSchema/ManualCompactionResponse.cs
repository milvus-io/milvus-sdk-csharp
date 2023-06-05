using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ManualCompactionResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("compactionID")]
    public long CompactionId { get; set; }
}
