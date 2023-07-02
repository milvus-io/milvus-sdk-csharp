using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Release a collection loaded before
/// </summary>
internal sealed class ReleaseCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want to release
    /// </remarks>
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

    internal static ReleaseCollectionRequest Create(string collectionName,string dbName)
    {
        return new ReleaseCollectionRequest(collectionName,dbName);
    }

    public Grpc.ReleaseCollectionRequest BuildGrpc()
    {
        this.Validate();
        return new Grpc.ReleaseCollectionRequest()
        {
            CollectionName = CollectionName,
            DbName = this.DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/collection/load",
            this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrEmpty(CollectionName, "Collection name cannot be null or empty");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private ===============================================================
    private ReleaseCollectionRequest(string collectionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
    #endregion
}
