using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Drop a collection
/// </summary>
internal sealed class DropCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The unique collection name in milvus.(Required)
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

    public static DropCollectionRequest Create(string collectionName, string dbName)
    {
        return new DropCollectionRequest(collectionName, dbName);
    }

    public Grpc.DropCollectionRequest BuildGrpc()
    {
        Validate();

        return new Grpc.DropCollectionRequest
        {
            CollectionName = CollectionName,
            DbName = DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/collection",
            payload: this);
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.NotNullOrWhiteSpace(DbName, "DbName cannot be null or empty");
    }

    #region Private =================================================================================
    public DropCollectionRequest(string collectionName, string dbName)
    {
        CollectionName = collectionName;
        DbName = dbName;
    }
    #endregion
}