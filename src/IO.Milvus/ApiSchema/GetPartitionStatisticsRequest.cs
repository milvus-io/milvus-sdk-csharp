using System;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get a partition's statistics
/// </summary>
public sealed class GetPartitionStatisticsRequest
{
    /// <summary>
    /// The collection name in milvus
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    [Obsolete("Not useful for now")]
    public string DbName { get; set; }

    /// <summary>
    /// The partition name you want to collect statistics
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }
}