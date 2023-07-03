using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Update password for a user
/// </summary>
internal sealed class UpdateCredentialRequest
{
    /// <summary>
    /// New password
    /// </summary>
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; }

    /// <summary>
    /// Old password
    /// </summary>
    [JsonPropertyName("oldPassword")]
    public string OldPassword { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; }
}