using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ShowPartitionsRequest
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
}
