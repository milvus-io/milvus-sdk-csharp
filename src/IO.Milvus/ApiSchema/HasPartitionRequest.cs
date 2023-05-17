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
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    /// <summary>
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }
}
