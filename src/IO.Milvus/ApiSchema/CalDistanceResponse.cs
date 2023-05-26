using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal class CalDistanceResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}
