using System;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete a Credential
/// </summary>
[Obsolete("Not useful for now")]
internal sealed class DeleteCredentialRequest
{
    /// <summary>
    /// Not useful for now
    /// </summary>
    [JsonPropertyName("username")]
    [Obsolete("Not useful for now")]
    public string Username { get;set; }
}
