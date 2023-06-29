using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus search result data
/// </summary>
public class MilvusSearchResultData
{
    /// <summary>
    /// Fields data
    /// </summary>
    [JsonPropertyName("fields_data")]
    [JsonConverter(typeof(MilvusFieldConverter))]
    public IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Ids
    /// </summary>
    [JsonPropertyName("ids")]
    public MilvusIds Ids { get; set; }

    /// <summary>
    /// Number of queries
    /// </summary>
    [JsonPropertyName("num_queries")]
    public long NumQueries { get; set; }

    /// <summary>
    /// Scores
    /// </summary>
    [JsonPropertyName("scores")]
    public IList<float> Scores { get; set; }

    /// <summary>
    /// TopK
    /// </summary>
    [JsonPropertyName("top_k")]
    public long TopK { get; set; }

    /// <summary>
    /// TopKs
    /// </summary>
    [JsonPropertyName("topks")]
    public IList<long> TopKs { get; set; }
}