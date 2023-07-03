using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetMetricsRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; }
}