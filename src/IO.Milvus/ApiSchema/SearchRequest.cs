using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Do a k nearest neighbors search with bool expression
/// </summary>
internal sealed class SearchRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Dsl
    /// </summary>
    [JsonPropertyName("dsl")]
    public string Dsl { get; set; }

    /// <summary>
    /// Dsl type
    /// </summary>
    [JsonPropertyName("dsl_type")]
    public int DslType { get; set; }

    /// <summary>
    /// Guarantee timestamp
    /// </summary>
    [JsonPropertyName("guarantee_timestamp")]
    public long GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Output fields
    /// </summary>
    [JsonPropertyName("output_fields")]
    public IList<string> OutputFields { get; set; }

    /// <summary>
    /// Partition names
    /// </summary>
    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    /// <summary>
    /// Placeholder group
    /// </summary>
    [JsonPropertyName("vectors")]
    public IList<List<float>> SearchVectors { get; set; } = new List<List<float>>();

    /// <summary>
    /// Search parameters
    /// </summary>
    [JsonPropertyName("search_params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> SearchParams { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Travel timestamp
    /// </summary>
    [JsonPropertyName("travel_timestamp")]
    public long TravelTimestamp { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}