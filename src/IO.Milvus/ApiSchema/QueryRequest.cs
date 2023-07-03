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
    /// expr
    /// </summary>
    [JsonPropertyName("expr")]
    public string Expr { get; set; }

    [JsonPropertyName("output_fields")]
    public IList<string> OutFields { get; set; }

    /// <summary>
    /// Guarantee timestamp
    /// </summary>
    [JsonPropertyName("guarantee_timestamp")]
    public long GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Partition names
    /// </summary>
    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    /// <summary>
    /// Travel timestamp
    /// </summary>
    [JsonPropertyName("travel_timestamp")]
    public long TravelTimestamp { get; set; }

    [JsonPropertyName("graceful_time")]
    public long GracefulTime { get; set; }

    [JsonPropertyName("consistency_level")]
    public MilvusConsistencyLevel ConsistencyLevel { get; set; }

    [JsonPropertyName("query_params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> QueryParams = new Dictionary<string, string>();

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}