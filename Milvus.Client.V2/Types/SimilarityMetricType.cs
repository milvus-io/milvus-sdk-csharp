namespace Milvus.Client.V2.Types;

/// <summary>
/// The metric type used to measure the distance/similarity between vectors.
/// </summary>
/// <remarks>
/// Aligned with the Milvus 2.6 metric types. The enum names are the parity unit; the wire value is a
/// string (e.g. <c>L2</c>) sent in the index/search requests.
/// </remarks>
public enum SimilarityMetricType
{
    /// <summary>
    /// An invalid metric type.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// Squared Euclidean distance (smaller is more similar). For float vectors.
    /// </summary>
    L2 = 1,

    /// <summary>
    /// Inner product (larger is more similar). For float vectors.
    /// </summary>
    Ip = 2,

    /// <summary>
    /// Cosine similarity (larger is more similar). For float vectors.
    /// </summary>
    Cosine = 3,

    /// <summary>
    /// Hamming distance (smaller is more similar). For binary vectors.
    /// </summary>
    Hamming = 4,

    /// <summary>
    /// Jaccard distance (smaller is more similar). For binary vectors.
    /// </summary>
    Jaccard = 5,

    /// <summary>
    /// Modified Jaccard distance (smaller is more similar). For binary vectors.
    /// </summary>
    MhJaccard = 6,

    /// <summary>
    /// The BM25 ranking metric for full-text search on sparse vectors.
    /// </summary>
    /// <remarks>
    /// Only valid for sparse float vectors (typically a BM25 function output field). Larger BM25 scores are
    /// more similar.
    /// </remarks>
    Bm25 = 7,

    /// <summary>
    /// Max-sim cosine similarity (larger is more similar). Equal to <see cref="MaxSimCosine" />.
    /// </summary>
    /// <remarks>
    /// The max-sim family applies to float vectors stored *inside* a struct field.
    /// </remarks>
    MaxSim = 8,

    /// <summary>
    /// Max-sim cosine similarity (larger is more similar).
    /// </summary>
    /// <remarks>
    /// Applies to float vectors inside a struct field.
    /// </remarks>
    MaxSimCosine = 9,

    /// <summary>
    /// Max-sim inner product.
    /// </summary>
    MaxSimIp = 10,

    /// <summary>
    /// Max-sim squared Euclidean distance.
    /// </summary>
    MaxSimL2 = 11,

    /// <summary>
    /// Max-sim Jaccard similarity (larger is more similar).
    /// </summary>
    /// <remarks>
    /// Applies to binary vectors inside a struct field.
    /// </remarks>
    MaxSimJaccard = 12,

    /// <summary>
    /// Max-sim Hamming similarity (smaller is more similar).
    /// </summary>
    /// <remarks>
    /// Applies to binary vectors inside a struct field.
    /// </remarks>
    MaxSimHamming = 13
}
