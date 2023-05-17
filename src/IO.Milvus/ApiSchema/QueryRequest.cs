using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// do a explicit record query by given expression. 
/// For example when you want to query by primary key.
/// </summary>
internal sealed class QueryRequest
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
    /// expr
    /// </summary>
    [JsonPropertyName("expr")]
    public string Expr { get; set; }

    /// <summary>
    /// Guarantee timestamp
    /// </summary>
    [JsonPropertyName("guarantee_timestamp")]
    public int GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Partition names
    /// </summary>
    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    /// <summary>
    /// Travel timestamp
    /// </summary>
    [JsonPropertyName("travel_timestamp")]
    public int TravelTimestamp { get; set; }
}