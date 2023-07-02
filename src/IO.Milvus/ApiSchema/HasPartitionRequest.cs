using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get if a partition exists
/// </summary>
internal sealed class HasPartitionRequest
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

    internal static HasPartitionRequest Create(string collectionName, string partitionName, string dbName)
    {
        return new HasPartitionRequest(collectionName, partitionName, dbName);
    }

    public Grpc.HasPartitionRequest BuildGrpc()
    {
        Validate();

        return new Grpc.HasPartitionRequest()
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName,
            DbName = DbName,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partition/existence",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PartitionName);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region Private =============================================================
    private HasPartitionRequest(string collectionName, string partitionName, string dbName)
    {
        CollectionName = collectionName;
        PartitionName = partitionName;
        DbName = dbName;
    }
    #endregion
}