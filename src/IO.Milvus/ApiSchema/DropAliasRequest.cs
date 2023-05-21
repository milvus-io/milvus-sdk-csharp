using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete an Alias
/// </summary>
internal sealed class DropAliasRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.DropAliasRequest>
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    public static DropAliasRequest Create(string alias)
    {
        return new DropAliasRequest(alias);
    }

    public Grpc.DropAliasRequest BuildGrpc()
    {
        return new Grpc.DropAliasRequest()
        {            
            Alias = Alias
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/alias",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(Alias, "Alias cannot be null or empty");
    }

    #region Private ============================================================================
    public DropAliasRequest(string alias)
    {
        Alias = alias;
    }
    #endregion
}