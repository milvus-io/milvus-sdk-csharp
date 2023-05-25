using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete a Credential
/// </summary>
internal sealed class DeleteCredentialRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.DeleteCredentialRequest>
{
    /// <summary>
    /// Not useful for now
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get;set; }

    public static DeleteCredentialRequest Create(string userName)
    {
        return new DeleteCredentialRequest(userName);
    }

    public Grpc.DeleteCredentialRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.DeleteCredentialRequest()
        {
            Username = this.Username
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/credential",
            payload:this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(Username, "Username cannot be null or empty");
    }

    #region Private ==================================================
    public DeleteCredentialRequest(string userName)
    {
        Username = userName;
    }
    #endregion
}
