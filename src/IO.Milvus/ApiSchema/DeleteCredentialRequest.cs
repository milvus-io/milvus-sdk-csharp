using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete a Credential
/// </summary>
internal sealed class DeleteCredentialRequest
{
    /// <summary>
    /// Not useful for now
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; }
}
