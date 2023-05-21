using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create an alias for a collection name
/// </summary>
internal class CreateAliasRequest
{
    public string Alias { get; set; }

    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    public static CreateAliasRequest Create(string collection,string alias)
    {
        return new CreateAliasRequest(collection,alias);
    }

    public Grpc.CreateAliasRequest BuildGrpc()
    {
        return new Grpc.CreateAliasRequest()
        {
            CollectionName = CollectionName,
            Alias = Alias
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/alias",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.ArgNotNullOrEmpty(Alias, "Alias cannot be null or empty");
    }

    #region Private =============================================
    private CreateAliasRequest(string collection, string alias)
    {
        CollectionName = collection;
        Alias = alias;
    }
    #endregion
}
