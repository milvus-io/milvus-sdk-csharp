namespace IO.Milvus;

/// <summary>
/// Metric Type
/// </summary>
/// <remarks>
/// <see href="https://milvus.io/docs/metric.md"/>
/// </remarks>
public enum MilvusMetricType
{
    /// <summary>
    /// Invalid
    /// </summary>
    Invalid,

    /// <summary>
    /// L2(Euclidean distance)
    /// </summary>
    /// <remarks>
    /// Essentially, Euclidean distance measures the length of a segment that connects 2 points.
    /// </remarks>
    L2,

    /// <summary>
    /// IP(Inner product)
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>IP is more useful if you are more interested in measuring the orientation but not the magnitude of the vectors.
    /// </item>
    /// <item>If you use IP to calculate embeddings similarities, you must normalize your embeddings. After normalization, the inner product equals cosine similarity.
    /// </item>
    /// </list>
    /// </remarks>
    IP,

    /// <summary>
    /// Hamming distance. Only supported for binary vectors.
    /// </summary>
    /// <remarks>
    /// <para>Hamming distance measures binary data strings. The distance between two strings of equal length is the number of bit positions at which the bits are different.
    /// </para>
    /// <para>For example, suppose there are two strings, 1101 1001 and 1001 1101.
    /// <para>11011001 ⊕ 10011101 = 01000100. Since, this contains two 1s, the Hamming distance, d (11011001, 10011101) = 2.
    /// </para>
    /// </para>
    /// </remarks>
    Hamming,

    /// <summary>
    /// Jaccard distance.
    /// </summary>
    /// <remarks>
    /// Jaccard similarity coefficient measures the similarity between two sample sets and is defined as the cardinality of the intersection of the defined sets divided by the cardinality of the union of them. It can only be applied to finite sample sets.
    /// </remarks>
    Jaccard,

    /// <summary>
    /// Tanimoto.
    /// </summary>
    /// <remarks>
    /// <para>For binary variables, the Tanimoto coefficient is equivalent to Jaccard distance:</para>
    /// <para>In Milvus, the Tanimoto coefficient is only applicable for a binary variable, and for binary variables, the Tanimoto coefficient ranges from 0 to +1 (where +1 is the highest similarity).</para>
    /// </remarks>
    Tanimoto,

    /// <summary>
    /// Sub structure
    /// </summary>
    /// <remarks>
    /// The Substructure is used to measure the similarity of a chemical structure and its substructure. When the value equals 0, this means the chemical structure in the database is the substructure of the target chemical structure.
    /// </remarks>
    Substructure,

    /// <summary>
    /// Super structure.
    /// </summary>
    /// <remarks>
    /// The Superstructure is used to measure the similarity of a chemical structure and its superstructure. When the value equals 0, this means the chemical structure in the database is the superstructure of the target chemical structure.
    /// </remarks>
    Superstructure,
}
