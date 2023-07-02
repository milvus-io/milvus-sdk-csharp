using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Update password for a user
/// </summary>
internal sealed class UpdateCredentialRequest
{
    /// <summary>
    /// UTC timestamps
    /// </summary>
    [JsonPropertyName("created_utc_timestamps")]
    [JsonIgnore]
    public long CreatedUTCTimestamps { get; set; }

    /// <summary>
    /// UTC timestamps
    /// </summary>
    [JsonPropertyName("modified_utc_timestamps")]
    [JsonIgnore]
    public long ModifiedUTCTimestamps { get; set; }

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

    public static UpdateCredentialRequest Create(string userName, string oldPassword, string newPassword)
    {
        return new UpdateCredentialRequest(userName, oldPassword, newPassword);
    }

    public Grpc.UpdateCredentialRequest BuildGrpc()
    {
        Validate();
        return new Grpc.UpdateCredentialRequest
        {
            NewPassword = NewPassword,
            OldPassword = OldPassword,
            Username = Username
        };
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();
        return HttpRequest.CreatePatchRequest(
            $"{ApiVersion.V1}/credential",
            payload: this);
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(Username);
        Verify.NotNullOrWhiteSpace(OldPassword);
        Verify.NotNullOrWhiteSpace(NewPassword);
    }

    #region Private ==================================================================
    public UpdateCredentialRequest(string userName, string oldPassword, string newPassword)
    {
        Username = userName;
        OldPassword = oldPassword;
        NewPassword = newPassword;
    }
    #endregion
}