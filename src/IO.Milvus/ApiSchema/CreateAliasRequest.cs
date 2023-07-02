using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create an alias for a collection name
/// </summary>
internal sealed class CreateAliasRequest
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static CreateAliasRequest Create(string collection, string alias, string dbName)
    {
        return new CreateAliasRequest(collection, alias, dbName);
    }

    public Grpc.CreateAliasRequest BuildGrpc()
    {
        return new Grpc.CreateAliasRequest()
        {
            CollectionName = CollectionName,
            Alias = Alias,
            DbName = DbName
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
        Verify.ArgNotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private =============================================
    private CreateAliasRequest(string collection, string alias, string dbName)
    {
        CollectionName = collection;
        Alias = alias;
        DbName = dbName;
    }
    #endregion
}
