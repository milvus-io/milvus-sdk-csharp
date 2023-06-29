using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get if a partition exists
/// </summary>
internal sealed class HasPartitionRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.HasPartitionRequest>
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Partition name
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

    internal static HasPartitionRequest Create(string collectionName, string partitionName,string dbName)
    {
        return new HasPartitionRequest(collectionName, partitionName, dbName);
    }

    public Grpc.HasPartitionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.HasPartitionRequest()
        {
            CollectionName = this.CollectionName,
            PartitionName = this.PartitionName,
            DbName = this.DbName,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partition/existence",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(PartitionName, "Milvus partition name cannot be null or empty.");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private =============================================================
    private HasPartitionRequest(string collectionName, string partitionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.PartitionName = partitionName;
        this.DbName = dbName;
    }
    #endregion
}