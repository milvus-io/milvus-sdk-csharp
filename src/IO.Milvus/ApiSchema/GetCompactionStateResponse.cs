using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetCompactionStateResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("state")]
    public MilvusCompactionState State { get; set; }

    [JsonPropertyName("executingPlanNo")]
    public long ExecutingPlanNo { get; set; }

    [JsonPropertyName("timeoutPlanNo")]
    public long TimeoutPlanNo { get; set; }

    [JsonPropertyName("completedPlanNo")]
    public long CompletedPlanNo { get; set; }
}