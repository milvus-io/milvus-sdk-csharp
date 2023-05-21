using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetIndexStateResponse
{
    [JsonPropertyName("fail_reason")]
    public string FailReason { get; set; }

    [JsonPropertyName("state")]
    public IndexState IndexState { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}