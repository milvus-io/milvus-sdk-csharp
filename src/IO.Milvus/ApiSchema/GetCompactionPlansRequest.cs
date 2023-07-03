using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetCompactionPlansRequest
{
    [JsonPropertyName("compactionID")]
    public long CompactionId { get; set; }
}