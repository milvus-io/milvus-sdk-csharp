using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.Milvus.Response;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create a new user and password
/// </summary>
internal sealed class CreateCredentialRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.CreateCredentialRequest>
{
    /// <summary>
    /// UTC timestamps
    /// </summary>
    [JsonPropertyName("created_utc_timestamps")]
    [JsonIgnore]
    public int CreatedUTCTimestamps { get; set; }

    /// <summary>
    /// UTC timestamps
    /// </summary>
    [JsonPropertyName("modified_utc_timestamps")]
    [JsonIgnore]
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

    public static CreateCredentialRequest Create(string username,string password)
    {
        return new CreateCredentialRequest(username, password);
    }

    public Grpc.CreateCredentialRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.CreateCredentialRequest()
        {
            Username = this.Username,
            Password = this.Password
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/credential",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(Username, "Username cannot be null or empty");
        Verify.ArgNotNullOrEmpty(Password, "Password cannot be null or empty");
    }

    #region Private =================================================================
    public CreateCredentialRequest(string username, string password)
    {
        this.Username = username;
        this.Password = password;
    }
    #endregion
}