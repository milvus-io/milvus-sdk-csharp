using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Response status
/// </summary>
public class ResponseStatus
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; }
}
