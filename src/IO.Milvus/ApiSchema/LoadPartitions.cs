using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class LoadPartitions
{
    /// <summary>
    /// Collection name in milvus
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
    /// The partition names you want to load
    /// </summary>
    public IList<string> PartitionNames { get; set; }

    /// <summary>
    /// The replicas number you would load, 1 by default
    /// </summary>
    public int ReplicaNumber { get; set; } = 1;
}