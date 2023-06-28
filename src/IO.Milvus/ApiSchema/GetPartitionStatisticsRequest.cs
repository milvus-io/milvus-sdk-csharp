using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get a partition's statistics
/// </summary>
internal sealed class GetPartitionStatisticsRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetPartitionStatisticsRequest>
{
    /// <summary>
    /// The collection name in milvus
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The partition name you want to collect statistics
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static GetPartitionStatisticsRequest Create(
        string collectionName,
        string partitionName,
        string dbName)
    {
        return new GetPartitionStatisticsRequest(collectionName, partitionName, dbName);
    }

    public Grpc.GetPartitionStatisticsRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetPartitionStatisticsRequest()
        {
            CollectionName = this.CollectionName,
            PartitionName = this.PartitionName,
            DbName = this.DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partition/statistics",
            payload:this);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.ArgNotNullOrEmpty(PartitionName, "Milvus partition name cannot be null or empty");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private ================================================================================
    public GetPartitionStatisticsRequest(string collectionName, string partitionName,string dbName)
    {
        this.CollectionName = collectionName;
        this.PartitionName = partitionName;
        this.DbName = dbName;
    }
    #endregion
}