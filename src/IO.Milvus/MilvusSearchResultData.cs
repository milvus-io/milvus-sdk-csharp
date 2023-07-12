using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus search result data
/// </summary>
public sealed class MilvusSearchResultData
{
    /// <summary>
    /// Fields data
    /// </summary>
    public IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Ids
    /// </summary>
    public MilvusIds Ids { get; set; }

    /// <summary>
    /// Number of queries
    /// </summary>
    public long NumQueries { get; set; }

    /// <summary>
    /// Scores
    /// </summary>
    public IList<float> Scores { get; set; }

    /// <summary>
    /// TopK
    /// </summary>
    public long TopK { get; set; }

    /// <summary>
    /// TopKs
    /// </summary>
    public IList<long> TopKs { get; set; }
}