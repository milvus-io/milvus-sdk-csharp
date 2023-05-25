using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Alter an alias
/// </summary>
internal class AlterAliasRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.AlterAliasRequest>
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    public static AlterAliasRequest Create(string collectionName,string alias)
    {
        return new AlterAliasRequest(collectionName,alias);
    }

    public Grpc.AlterAliasRequest BuildGrpc()
    {
        return new Grpc.AlterAliasRequest()
        {
            CollectionName = CollectionName,
            Alias = Alias
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreatePatchRequest(
            $"{ApiVersion.V1}/alias",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.ArgNotNullOrEmpty(Alias, "Alias cannot be null or empty");
    }

    #region Private ================================================================================
    private AlterAliasRequest(string collection, string alias)
    {
        CollectionName = collection;
        Alias = alias;
    }
    #endregion
}
