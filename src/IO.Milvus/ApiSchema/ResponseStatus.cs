using IO.Milvus.Grpc;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Response status
/// </summary>
internal sealed class ResponseStatus
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("error_code")]
    public ErrorCode ErrorCode { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; }
}
