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
        this.Validate();
        return new Grpc.UpdateCredentialRequest
        {
            NewPassword = NewPassword,
            OldPassword = OldPassword,
            Username = Username
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();
        return HttpRequest.CreatePatchRequest(
            $"{ApiVersion.V1}/credential",
            payload: this);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(Username, "Username cannot be null or empty");
        Verify.ArgNotNullOrEmpty(OldPassword, "OldPassword cannot be null or empty");
        Verify.ArgNotNullOrEmpty(NewPassword, "NewPassword cannot be null or empty");
    }

    #region Private ==================================================================
    public UpdateCredentialRequest(string userName, string oldPassword, string newPassword)
    {
        this.Username = userName;
        this.OldPassword = oldPassword;
        this.NewPassword = newPassword;
    }
    #endregion
}