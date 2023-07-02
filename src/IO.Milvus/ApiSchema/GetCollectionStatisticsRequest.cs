using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Security;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get a collection's statistics
/// </summary>
internal sealed class GetCollectionStatisticsRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want get statistics
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

    public static GetCollectionStatisticsRequest Create(string collectionName, string dbName)
    {
        return new GetCollectionStatisticsRequest(collectionName, dbName);
    }

    public Grpc.GetCollectionStatisticsRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetCollectionStatisticsRequest()
        {
            CollectionName = this.CollectionName,
            DbName = this.DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection/statistics",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private ==================================================
    public GetCollectionStatisticsRequest(string collectionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
    #endregion
}