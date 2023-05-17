using System;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create a partition
/// </summary>
internal sealed class CreatePartitionRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Obsolete("Not useful for now")]
 
    public string DbName { get; set; }
}
