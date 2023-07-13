namespace IO.Milvus;

// ReSharper disable IdentifierTypo

/// <summary>
/// Milvus index type.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <listheader>Usage:</listheader>
/// <item>For zilliz cloud:</item>
/// <item><see cref="AutoIndex"/></item>
/// <item>For For floating point vectors.:</item>
/// <item><see cref="Flat"/></item>
/// <item><see cref="IvfFlat"/></item>
/// <item><see cref="IvfSq8"/></item>
/// <item><see cref="IvfPq"/></item>
/// <item><see cref="Hnsw"/></item>
/// <item><see cref="Annoy"/></item>
/// <item>For binary vectors:</item>
/// <item><see cref="BinFlat"/></item>
/// <item><see cref="BinIvfFlat"/></item>
/// </list>
/// For more information, please refer to <see href="https://milvus.io/docs/v2.0.0/index.md#Index"/>
/// </remarks>
public enum MilvusIndexType
{
    /// <summary>
    /// Invalid.
    /// </summary>
    Invalid,

    /// <summary>
    /// Flat.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For vector similarity search applications that require perfect accuracy and depend on relatively small
    /// (million-scale) datasets, the FLAT index is a good choice. FLAT does not compress vectors, and is the only index
    /// that can guarantee exact search results. Results from FLAT can also be used as a point of comparison for results
    /// produced by other indexes that have less than 100% recall.
    /// </para>
    /// <para>
    /// FLAT is accurate because it takes an exhaustive approach to search, which means for each query the target input
    /// is compared to every vector in a dataset. This makes FLAT the slowest index on our list, and poorly suited for
    /// querying massive vector data. There are no parameters for the FLAT index in Milvus, and using it does not
    /// require data training or additional storage.
    /// </para>
    /// </remarks>
    Flat,

    /// <summary>
    /// IVF_FLAT.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IVF_FLAT divides vector data into nlist cluster units, and then compares distances between the target input
    /// vector and the center of each cluster. Depending on the number of clusters the system is set to query (nprobe),
    /// similarity search results are returned based on comparisons between the target input and the vectors in the most
    /// similar cluster(s) only — drastically reducing query time.
    /// </para>
    /// <para>
    /// By adjusting nprobe, an ideal balance between accuracy and speed can be found for a given scenario. Results from
    /// the IVF_FLAT performance test demonstrate that query time increases sharply as both the number of target input
    /// vectors (nq), and the number of clusters to search (nprobe), increase.</para>
    /// <para>
    /// IVF_FLAT is the most basic IVF index, and the encoded data stored in each unit is consistent with the original
    /// data.
    /// </para>
    /// </remarks>
    IvfFlat,

    /// <summary>
    /// IVF_PQ
    /// </summary>
    /// <remarks>
    /// <para>
    /// PQ (Product Quantization) uniformly decomposes the original high-dimensional vector space into Cartesian
    /// products of m low-dimensional vector spaces, and then quantizes the decomposed low-dimensional vector spaces.
    /// Instead of calculating the distances between the target vector and the center of all the units, product
    /// quantization enables the calculation of distances between the target vector and the clustering center of each
    /// low-dimensional space and greatly reduces the time complexity and space complexity of the algorithm.
    /// </para>
    /// <para>
    /// IVF_PQ performs IVF index clustering before quantizing the product of vectors. Its index file is even smaller
    /// than IVF_SQ8, but it also causes a loss of accuracy during searching vectors.
    /// </para>
    /// </remarks>
    IvfPq,

    /// <summary>
    /// IVF_SQ8
    /// </summary>
    /// <remarks>
    /// <para>
    /// IVF_FLAT does not perform any compression, so the index files it produces are roughly the same size as the
    /// original, raw non-indexed vector data. For example, if the original 1B SIFT dataset is 476 GB, its IVF_FLAT
    /// index files will be slightly larger (~470 GB). Loading all the index files into memory will consume 470 GB of
    /// storage.
    /// </para>
    /// <para>
    /// When disk, CPU, or GPU memory resources are limited, IVF_SQ8 is a better option than IVF_FLAT. This index type
    /// can convert each FLOAT (4 bytes) to UINT8 (1 byte) by performing scalar quantization. This reduces disk, CPU,
    /// and GPU memory consumption by 70–75%. For the 1B SIFT dataset, the IVF_SQ8 index files require just 140 GB of
    /// storage.
    /// </para>
    /// </remarks>
    IvfSq8,

    /// <summary>
    /// IVF_HNSW
    /// </summary>
    /// <remarks>
    /// For floating point vectors.
    /// </remarks>
    IvfHnsw,

    /// <summary>
    /// HNSW
    /// </summary>
    /// <remarks>
    /// <para>
    /// HNSW (Hierarchical Navigable Small World Graph) is a graph-based indexing algorithm. It builds a multi-layer
    /// navigation structure for an image according to certain rules. In this structure, the upper layers are more
    /// sparse and the distances between nodes are farther; the lower layers are denser and the distances between nodes
    /// are closer. The search starts from the uppermost layer, finds the node closest to the target in this layer, and
    /// then enters the next layer to begin another search. After multiple iterations, it can quickly approach the
    /// target position.
    /// </para>
    /// <para>
    /// In order to improve performance, HNSW limits the maximum degree of nodes on each layer of the graph to M. In
    /// addition, you can use efConstruction (when building index) or ef (when searching targets) to specify a search
    /// range.
    /// </para>
    /// </remarks>
    Hnsw,

    /// <summary>
    /// RHNSW_FLAT
    /// </summary>
    RhnswFlat,

    /// <summary>
    /// RHNSW_PQ
    /// </summary>
    RhnswPq,

    /// <summary>
    /// RHNSW_SQ
    /// </summary>
    RhnswSq,

    /// <summary>
    /// BIN_FLAT
    /// </summary>
    /// <remarks>
    /// <para>
    /// ANNOY (Approximate Nearest Neighbors Oh Yeah) is an index that uses a hyperplane to divide a high-dimensional
    /// space into multiple subspaces, and then stores them in a tree structure.
    /// </para>
    /// <para>
    /// There are just two main parameters needed to tune ANNOY: the number of trees n_trees and the number of nodes to
    /// inspect during searching search_k.
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// n_trees is provided during build time and affects the build time and the index size. A larger value will give
    /// more accurate results, but larger indexes.
    /// </item>
    /// <item>
    /// search_k is provided in runtime and affects the search performance. A larger value will give more accurate
    /// results, but will take longer time to return.
    /// </item>
    /// </list>
    /// <para>
    /// If search_k is not provided, it will default to n * n_trees where n is the number of approximate nearest
    /// neighbors. Otherwise, search_k and n_trees are roughly independent, i.e. the value of n_trees will not affect
    /// search time if search_k is held constant and vice versa. Basically it's recommended to set n_trees as large as
    /// possible given the amount of memory you can afford, and it's recommended to set search_k as large as possible
    /// given the time constraints you have for the queries.
    /// </para>
    /// </remarks>
    Annoy,

    /// <summary>
    /// Only supported for binary vectors
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    BinFlat,

    /// <summary>
    /// BIN_IVF_FLAT
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    BinIvfFlat,

    /// <summary>
    /// TRIE
    /// </summary>
    Trie,

    /// <summary>
    /// <para>
    /// AUTOINDEX is a proprietary index type available on Zilliz Cloud for index auto-optimization.
    /// </para>
    /// <para>
    /// And now, it can be used in milvus v2.2.9
    /// </para>
    /// </summary>
    /// <remarks>
    /// Currently, Zilliz Cloud does not allow indexes on scalar fields.
    /// <see href="https://github.com/milvus-io/milvus/pull/24443"/>
    /// </remarks>
    AutoIndex
}
