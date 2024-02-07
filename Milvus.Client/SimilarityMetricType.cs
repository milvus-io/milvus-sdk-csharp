namespace Milvus.Client;

#pragma warning disable CS1591 // Missing XML comments. Documentation is missing for some of the metric types below.

/// <summary>
/// Similarity metrics are used to measure similarities among vectors.
/// Choosing a good distance metric helps improve the classification and clustering performance significantly.
/// </summary>
/// <remarks>
/// For more details, see <see href="https://milvus.io/docs/metric.md" />.
/// </remarks>
public enum SimilarityMetricType
{
    Invalid,

    /// <summary>
    /// Euclidean distance (L2). Essentially, Euclidean distance measures the length of a segment that connects 2
    /// points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This metric type is valid for float vectors only.
    /// </para>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Euclidean-distance-L2" />.
    /// </para>
    /// </remarks>
    L2,

    /// <summary>
    /// Inner product (IP).
    /// </summary>
    /// <remarks>
    /// <para>
    /// IP is more useful if you are more interested in measuring the orientation but not the magnitude of the vectors.
    /// </para>
    /// <para>
    /// If you use IP to calculate embeddings similarities, you must normalize your embeddings. After
    /// normalization, the inner product equals cosine similarity.
    /// </para>
    /// <para>
    /// This metric type is valid for float vectors only.
    /// </para>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Inner-product-IP" />.
    /// </para>
    /// </remarks>
    Ip,

    /// <summary>
    /// Jaccard similarity coefficient measures the similarity between two sample sets and is defined as the cardinality
    /// of the intersection of the defined sets divided by the cardinality of the union of them. It can only be applied
    /// to finite sample sets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This metric type is valid for binary vectors only.
    /// </para>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Jaccard-distance" />.
    /// </para>
    /// </remarks>
    Jaccard,

    /// <summary>
    /// Tanimoto distance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Milvus, the Tanimoto coefficient is only applicable for a binary variable, and for binary variables, the
    /// Tanimoto coefficient ranges from 0 to +1 (where +1 is the highest similarity).
    /// </para>
    /// <para>
    /// This metric type is valid for binary vectors only.
    /// </para>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Tanimoto-distance" />.
    /// </para>
    /// </remarks>
    Tanimoto,

    /// <summary>
    /// Hamming distance measures binary data strings.
    /// The distance between two strings of equal length is the number of bit positions at which the bits are different.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, suppose there are two strings, <c>1101 1001</c> and <c>1001 1101</c>.
    /// </para>
    /// <para>
    /// <c>11011001 ⊕ 10011101 = 01000100</c>. Since, this contains two 1s, the Hamming distance,
    /// <c>d (11011001, 10011101) = 2</c>.
    /// </para>
    /// <para>
    /// This metric type is valid for binary vectors only.
    /// </para>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Hamming-distance" />.
    /// </para>
    /// </remarks>
    Hamming,

    /// <summary>
    /// <see cref="Superstructure" /> is used to measure the similarity of a chemical structure and its superstructure.
    /// When the value equals 0, this means the chemical structure in the database is the superstructure of the target
    /// chemical structure.
    /// </summary>
    /// <para>
    /// This metric type is valid for binary vectors only.
    /// </para>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Superstructure" />.
    /// </remarks>
    Superstructure,

    /// <summary>
    /// <see cref="Substructure" /> is used to measure the similarity of a chemical structure and its substructure.
    /// When the value equals 0, this means the chemical structure in the database is the substructure of the target
    /// chemical structure.
    /// </summary>
    /// <para>
    /// This metric type is valid for binary vectors only.
    /// </para>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Substructure" />.
    /// </remarks>
    Substructure,

    /// <summary>
    /// <see cref="Cosine" /> is used to measure the cosine of the angle between two non-zero vectors in a multidimensional space,
    /// reflecting the degree of similarity between them. The value ranges from -1 to 1, where 1 indicates that the vectors
    /// are identical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This metric is particularly useful for measuring the similarity in text analysis and other types of data where 
    /// the magnitude of the vectors does not matter as much as the direction. In these cases, cosine similarity can 
    /// effectively capture the similarity between vectors, regardless of their size.
    /// </para>
    /// <para>
    /// It's widely used in applications involving natural language processing, search engines, and recommendation systems 
    /// to calculate the similarity between documents or user preferences.
    /// </para>
    /// <para>
    /// This metric type is valid for float vectors only.
    /// </para>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/metric.md#Cosine-Similarity" />.
    /// </para>
    /// </remarks>
    Cosine,

}
