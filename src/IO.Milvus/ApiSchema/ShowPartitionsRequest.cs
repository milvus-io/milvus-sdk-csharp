using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ShowPartitionsRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.ShowPartitionsRequest>
{
    /// <summary>
    /// The collection name you want to describe, 
    /// you can pass collection_name or collectionID
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The collection id in milvus
    /// </summary>
    [JsonPropertyName("collectionID")]
    public int CollectionId { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    [Obsolete("Not useful for now")]
    public string DbName { get; set; }

    /// <summary>
    /// Partition names
    /// </summary>
    /// <remarks>
    /// When type is InMemory, 
    /// will return these partitions inMemory_percentages.(Optional)
    /// </remarks>
    [JsonPropertyName("partition_names")]
    public IList<int> PartitionNames { get; set; }

    /// <summary>
    /// Decide return Loaded partitions or All partitions(Optional) 
    /// </summary>
    /// <remarks>
    /// Optional
    /// </remarks>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    internal static ShowPartitionsRequest Create(string collectionName)
    {
        return new ShowPartitionsRequest(collectionName);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partitions",
            payload: this
            );
    }

    public Grpc.ShowPartitionsRequest BuildGrpc()
    {
        return new Grpc.ShowPartitionsRequest()
        {
            CollectionName = CollectionName
        };
    }

    #region Private =============================================================
    private ShowPartitionsRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}

/// <summary>
/// Milvus partition
/// </summary>
/// <remarks>
/// Milvus allows you to divide the bulk of vector data into a small number of partitions. 
/// Search and other operations can then be limited to one partition to improve the performance.
/// </remarks>
public class MilvusPartition
{
    /// <summary>
    /// Construct a milvus partition.
    /// </summary>
    /// <param name="partitionId">Partition id.</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="createdUtcTimestamp">Created datetime.</param>
    /// <param name="inMemoryPercentages">Load percentage on query node.</param>
    public MilvusPartition(
        long partitionId,
        string partitionName,
        DateTime createdUtcTimestamp,
        long inMemoryPercentages)
    {
        PartitionId = partitionId;
        PartitionName = partitionName;
        CreatedUtcTimestamp = createdUtcTimestamp;
        InMemoryPercentages = inMemoryPercentages;
    }

    /// <summary>
    /// Partition id
    /// </summary>
    public long PartitionId { get; }

    /// <summary>
    /// Partition name
    /// </summary>
    public string PartitionName { get; }

    /// <summary>
    /// Load percentage on query node.
    /// </summary>
    public long InMemoryPercentages { get; }

    /// <summary>
    /// Create utc time
    /// </summary>
    /// <remarks>
    /// If you want to get a local time, you can use <see cref="DateTime.ToLocalTime"/>
    /// </remarks>
    public DateTime CreatedUtcTimestamp { get; }
}
