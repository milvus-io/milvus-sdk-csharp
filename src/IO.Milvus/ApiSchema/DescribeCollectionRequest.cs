using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Describe a collection
/// </summary>
internal sealed class DescribeCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The collection ID you want to describe
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// TimeStamp
    /// </summary>
    /// <remarks>
    /// If time_stamp is not zero, will return true when time_stamp >= created collection timestamp, otherwise will return false.
    /// </remarks>
    [JsonPropertyName("time_stamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    /// <summary>
    /// Create a description collection request.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    public static DescribeCollectionRequest Create(string collectionName, string dbName)
    {
        return new DescribeCollectionRequest(collectionName, dbName);
    }

    public DescribeCollectionRequest WithCollectionId(int collectionId)
    {
        CollectionId = collectionId;
        return this;
    }

    public DescribeCollectionRequest WithTimestamp(int timestamp)
    {
        Timestamp = timestamp;
        return this;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection",
            payload: this
            );
    }

    public Grpc.DescribeCollectionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.DescribeCollectionRequest()
        {
            CollectionID = this.CollectionId,
            CollectionName = this.CollectionName,
            TimeStamp = (ulong)this.Timestamp,
            DbName = this.DbName,
        };
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region private ================================================================================

    private DescribeCollectionRequest(string collectionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
    #endregion
}