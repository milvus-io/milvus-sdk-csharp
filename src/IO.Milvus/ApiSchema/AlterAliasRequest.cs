using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Alter an alias
/// </summary>
internal sealed class AlterAliasRequest:
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

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static AlterAliasRequest Create(string collectionName,string alias,string dbName)
    {
        return new AlterAliasRequest(collectionName,alias,dbName);
    }

    public Grpc.AlterAliasRequest BuildGrpc()
    {
        return new Grpc.AlterAliasRequest()
        {
            CollectionName = this.CollectionName,
            Alias = this.Alias,
            DbName = this.DbName
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
        Verify.ArgNotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private ================================================================================
    private AlterAliasRequest(string collection, string alias, string dbName)
    {
        this.CollectionName = collection;
        this.Alias = alias;
        this.DbName = dbName;
    }
    #endregion
}
