using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetMetricsResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    /// <summary>
    /// Metrics from which component.
    /// </summary>
    [JsonPropertyName("component_name")]
    public string ComponentName { get; set; }

    /// <summary>
    /// Response is of jsonic format.
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; }
}