namespace IO.Milvus;

/// <summary>
/// Metric Type
/// </summary>
public enum MilvusMetricType
{
    /// <summary>
    /// Invalid
    /// </summary>
    Invalid,
    
    /// <summary>
    /// L2
    /// </summary>
    L2,

    /// <summary>
    /// IP
    /// </summary>
    IP,

    /// <summary>
    /// Only supported for binary vectors.
    /// </summary>
    Hamming,

    /// <summary>
    /// Jaccard.
    /// </summary>
    Jaccard,

    /// <summary>
    /// Tanimoto.
    /// </summary>
    Tanimoto,

    /// <summary>
    /// Sub structure
    /// </summary>
    Substructure,

    /// <summary>
    /// Super structure.
    /// </summary>
    Superstructure,
}
