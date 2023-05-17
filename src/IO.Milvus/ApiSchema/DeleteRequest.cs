using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete rows of data entities from a collection by given expresssion
/// </summary>
internal class DeleteRequest
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
    /// Expr
    /// </summary>
    [JsonPropertyName("expr")]
    public string Expr { get; set; }

    /// <summary>
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    /// <summary>
    /// Hash keys
    /// </summary>
    [JsonPropertyName("hash_keys")]
    public IList<int> HashKeys { get; set; }
}