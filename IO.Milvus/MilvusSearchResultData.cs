namespace IO.Milvus;

/// <summary>
/// Milvus search result data
/// </summary>
public sealed class MilvusSearchResultData
{
    /// <summary>
    /// Fields data
    /// </summary>
    public required IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Ids
    /// </summary>
    public required MilvusIds Ids { get; set; }

    /// <summary>
    /// Number of queries
    /// </summary>
    public long NumQueries { get; set; }

    /// <summary>
    /// Scores
    /// </summary>
    public required IList<float> Scores { get; set; }

    /// <summary>
    /// TopK
    /// </summary>
    public long TopK { get; set; }

    /// <summary>
    /// TopKs
    /// </summary>
    public required IList<long> TopKs { get; set; }
}