using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create a new user and password
/// </summary>
internal sealed class CreateCredentialRequest
{
    /// <summary>
    /// UTC timestamps
    /// </summary>
    [JsonPropertyName("created_utc_timestamps")]
    public int CreatedUTCTimestamps { get; set; }

    /// <summary>
    /// UTC timestamps
    /// </summary>
    [JsonPropertyName("modified_utc_timestamps")]
    public int ModifiedUTCTimestamps { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; }

    /// <summary>
    /// New password
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; }
}