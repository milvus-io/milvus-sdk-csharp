namespace Milvus.Client.V2.Types;

/// <summary>
/// The index type used when creating an index on a field.
/// </summary>
/// <remarks>
/// Aligned with the Milvus 2.6 index types. The enum names are the parity unit; the wire value is a
/// string (e.g. <c>IVF_FLAT</c>) sent in the index create request.
/// </remarks>
public enum IndexType
{
    /// <summary>
    /// An invalid index type.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// A brute-force index that compares each query vector against all vectors.
    /// </summary>
    /// <remarks>
    /// CPU index for float vectors; always exact (no quantization loss). Good for small datasets.
    /// </remarks>
    Flat = 1,

    /// <summary>
    /// An inverted-file index that partitions the vector space into clusters.
    /// </summary>
    /// <remarks>
    /// CPU index for float vectors; the <c>nlist</c> extra param controls cluster count.
    /// </remarks>
    IvfFlat = 2,

    /// <summary>
    /// An IVF index with scalar quantization.
    /// </summary>
    IvfSq8 = 3,

    /// <summary>
    /// An IVF index with product quantization.
    /// </summary>
    IvfPq = 4,

    /// <summary>
    /// A graph-based index using Hierarchical Navigable Small World graphs.
    /// </summary>
    /// <remarks>
    /// CPU index for float vectors; the <c>M</c> and <c>efConstruction</c> extra params tune recall vs cost.
    /// </remarks>
    Hnsw = 5,

    /// <summary>
    /// An HNSW index with scalar quantization.
    /// </summary>
    HnswSq = 6,

    /// <summary>
    /// An HNSW index with product quantization.
    /// </summary>
    HnswPq = 7,

    /// <summary>
    /// An HNSW index with product quantization and residual coding.
    /// </summary>
    HnswPrq = 8,

    /// <summary>
    /// A disk-based index for very large datasets.
    /// </summary>
    DiskAnn = 9,

    /// <summary>
    /// An index type automatically chosen by the server.
    /// </summary>
    AutoIndex = 10,

    /// <summary>
    /// A graph-based index from Microsoft (ANNS).
    /// </summary>
    Scann = 11,

    /// <summary>
    /// A GPU-accelerated IVF index.
    /// </summary>
    GpuIvfFlat = 12,

    /// <summary>
    /// A GPU-accelerated IVF index with product quantization.
    /// </summary>
    GpuIvfPq = 13,

    /// <summary>
    /// A GPU brute-force index.
    /// </summary>
    GpuBruteForce = 14,

    /// <summary>
    /// A GPU-accelerated CAGRA graph index.
    /// </summary>
    GpuCagra = 15,

    /// <summary>
    /// A binary vector flat index.
    /// </summary>
    /// <remarks>
    /// CPU index for binary vectors; exact (no quantization loss).
    /// </remarks>
    BinFlat = 16,

    /// <summary>
    /// A binary vector inverted-file index.
    /// </summary>
    /// <remarks>
    /// CPU index for binary vectors; the <c>nlist</c> extra param controls cluster count.
    /// </remarks>
    BinIvfFlat = 17,

    /// <summary>
    /// A trie index for string fields.
    /// </summary>
    /// <remarks>
    /// Used for exact-match prefix queries on VarChar fields.
    /// </remarks>
    Trie = 18,

    /// <summary>
    /// A sorted-list index for numeric fields.
    /// </summary>
    StlSort = 19,

    /// <summary>
    /// An inverted index for scalar fields (works for all scalar fields except JSON).
    /// </summary>
    Inverted = 20,

    /// <summary>
    /// A bitmap index for scalar fields (works for all scalar fields except JSON, FLOAT and DOUBLE).
    /// </summary>
    Bitmap = 21,

    /// <summary>
    /// An inverted index for sparse vectors.
    /// </summary>
    SparseInvertedIndex = 22,

    /// <summary>
    /// A sparse vector index using the WAND algorithm.
    /// </summary>
    SparseWand = 23,

    /// <summary>
    /// An n-gram full-text index for string fields.
    /// </summary>
    Ngram = 24,

    /// <summary>
    /// An R-tree index for spatial/geographic data.
    /// </summary>
    Rtree = 25,

    /// <summary>
    /// A full-text index using the FM algorithm.
    /// </summary>
    FmIndex = 26,

    /// <summary>
    /// A hybrid index (combined BITMAP + INVERTED).
    /// </summary>
    Hybrid = 27,

    /// <summary>
    /// A vector index automatically selected by the server for the target vector type.
    /// </summary>
    VecIndex = 28,

    /// <summary>
    /// An IVF index with RaBitQ quantization.
    /// </summary>
    IvfRabitq = 29,

    /// <summary>
    /// A MinHash LSH index for binary vectors holding MinHash signatures, used for Jaccard-similarity search.
    /// </summary>
    MinHashLsh = 30,

    /// <summary>
    /// An AISAQ index for scalar/vector fields.
    /// </summary>
    /// <remarks>
    /// Adaptive Indexing with Scalar And Quantization; supports scalar fields and low-dimensional vectors.
    /// </remarks>
    Aisaq = 31
}
