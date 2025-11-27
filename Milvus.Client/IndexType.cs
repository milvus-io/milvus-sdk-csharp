namespace Milvus.Client;

#pragma warning disable CS1591 // Missing XML comments. Documentation is missing for some of the index types below.

/// <summary>
/// Indexing is the process of efficiently organizing data, and it plays a major role in making similarity search useful
/// by dramatically accelerating time-consuming queries on large datasets. To improve query performance, you can specify
/// an index type for each vector field.
/// </summary>
public enum IndexType
{
    Invalid = 0,

    /// <summary>
    /// <para>
    /// For vector similarity search applications that require perfect accuracy and depend on relatively small
    /// (million-scale) datasets, the <see cref="Flat" /> index is a good choice. <see cref="Flat" /> does not compress
    /// vectors, and is the only index that can guarantee exact search results. Results from <see cref="Flat" /> can
    /// also be used as a point of comparison for results produced by other indexes that have less than 100% recall.
    /// </para>
    /// <para>
    /// FLAT is accurate because it takes an exhaustive approach to search, which means for each query the target input
    /// is compared to every vector in a dataset. This makes FLAT the slowest index on our list, and poorly suited for
    /// querying massive vector data. There are no parameters for the FLAT index in Milvus, and using it does not
    /// require data training or additional storage.
    /// </para>
    /// </summary>
    Flat,

    /// <summary>
    /// <para>
    /// Divides vector data into <c>nlist</c> cluster units, and then compares distances between the target input
    /// vector and the center of each cluster. Depending on the number of clusters the system is set to query
    /// (<c>nprobe</c>), similarity search results are returned based on comparisons between the target input and the
    /// vectors in the most similar cluster(s) only — drastically reducing query time.
    /// </para>
    /// <para>
    /// By adjusting <c>nprobe</c>, an ideal balance between accuracy and speed can be found for a given scenario.
    /// Results from the <see cref="IvfFlat" /> performance test demonstrate that query time increases sharply as both
    /// the number of target input vectors (<c>nq</c>), and the number of clusters to search (<c>nprobe</c>), increase.
    /// </para>
    /// <para>
    /// <see cref="IvfFlat" /> is the most basic IVF index, and the encoded data stored in each unit is consistent with
    /// the original data.
    /// </para>
    /// </summary>
    IvfFlat,

    /// <summary>
    /// <para>
    /// <see cref="IvfFlat" /> does not perform any compression, so the index files it produces are roughly the same
    /// size as the original, raw non-indexed vector data. For example, if the original 1B SIFT dataset is 476 GB, its
    /// <see cref="IvfFlat" /> index files will be slightly larger (~470 GB). Loading all the index files into memory
    /// will consume 470 GB of storage.
    /// </para>
    /// <para>
    /// When disk, CPU, or GPU memory resources are limited, <see cref="IvfSq8" /> is a better option than
    /// <see cref="IvfFlat" />. This index type can convert each <c>FLOAT</c> (4 bytes) to <c>UINT8</c> (1 byte) by
    /// performing scalar quantization. This reduces disk, CPU, and GPU memory consumption by 70–75%.
    /// For the 1B SIFT dataset, the <see cref="IvfSq8" /> index files require just 140 GB of
    /// storage.
    /// </para>
    /// </summary>
    IvfSq8,

    /// <summary>
    /// <para>
    /// <see cref="IvfPq" /> (Product Quantization) uniformly decomposes the original high-dimensional vector space into
    /// Cartesian products of m low-dimensional vector spaces, and then quantizes the decomposed low-dimensional vector
    /// spaces. Instead of calculating the distances between the target vector and the center of all the units, product
    /// quantization enables the calculation of distances between the target vector and the clustering center of each
    /// low-dimensional space and greatly reduces the time complexity and space complexity of the algorithm.
    /// </para>
    /// <para>
    /// <see cref="IvfPq" /> performs IVF index clustering before quantizing the product of vectors. Its index file is
    /// even smaller than <see cref="IvfSq8" />, but it also causes a loss of accuracy during searching vectors.
    /// </para>
    /// </summary>
    IvfPq,

    /// <summary>
    /// <see cref="Hnsw"/> (Hierarchical Navigable Small World Graph) is a graph-based indexing algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Hnsw"/> builds a multi-layer navigation structure for an image according to certain rules. In this
    /// structure, the upper layers are more sparse and the distances between nodes are farther; the lower layers are
    /// denser and the distances between nodes are closer. The search starts from the uppermost layer, finds the node
    /// closest to the target in this layer, and then enters the next layer to begin another search. After multiple
    /// iterations, it can quickly approach the target position.
    /// </para>
    /// <para>
    /// In order to improve performance, <see cref="Hnsw" /> limits the maximum degree of nodes on each layer of the
    /// graph to M. In addition, you can use efConstruction (when building index) or ef (when searching targets) to
    /// specify a search range.
    /// </para>
    /// </remarks>
    Hnsw,

    /// <summary>
    /// <para>
    /// SCANN (Scalable Nearest Neighbors) is a quantization-based index similar to <see cref="IvfPq" /> in terms of
    /// vector clustering and product quantization. SCANN demonstrates a 20% performance improvement compared to HNSW
    /// and a 7-fold increase compared to IVF-FLAT in multiple benchmark tests.
    /// </para>
    /// <para>
    /// SCANN offers a faster index-building process than <see cref="IvfPq" />. However, using SCANN may result in a
    /// potential loss of precision and therefore requires refinement using the raw vectors (controlled by the
    /// <c>with_raw_data</c> parameter).
    /// </para>
    /// <para>
    /// Build parameters: <c>nlist</c> (number of cluster units, range [1, 65536]),
    /// <c>with_raw_data</c> (whether to include raw data in the index, defaults to true).
    /// </para>
    /// <para>
    /// Search parameters: <c>nprobe</c> (number of units to query), <c>reorder_k</c> (number of candidate units to query).
    /// </para>
    /// </summary>
    /// <remarks>
    /// Introduced in Milvus v2.3.0. Suitable for scenarios requiring very high-speed queries with the highest possible
    /// recall rate and large memory resources.
    /// </remarks>
    Scann,

    /// <summary>
    /// SCANN (Score-aware quantization loss) is similar to <see cref="IvfPq" /> in terms of vector clustering and
    /// product quantization. What makes them different lies in the implementation details of product quantization and
    /// the use of SIMD (Single-Instruction / Multi-data) for efficient calculation.
    /// </summary>
    DiskANN,

    /// <summary>
    /// A graph-based index optimized for GPUs, GPU_CAGRA performs well on inference GPUs. It's best suited for
    /// situations with a small number of queries, where training GPUs with lower memory frequency may not yield optimal
    /// results.
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/gpu_index.md" />
    /// </remarks>
    GpuCagra,

    /// <summary>
    /// This quantization-based index organizes vector data into clusters and employs product quantization for efficient
    /// search. It is ideal for scenarios requiring fast queries and can manage limited memory resources while balancing
    /// accuracy and speed..
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/gpu_index.md" />
    /// </remarks>
    GpuIvfFlat,

    /// <summary>
    /// This quantization-based index organizes vector data into clusters and employs product quantization for efficient
    /// search. It is ideal for scenarios requiring fast queries and can manage limited memory resources while balancing
    /// accuracy and speed..
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/gpu_index.md" />
    /// </remarks>
    GpuIvfPq,

    /// <summary>
    /// This index is tailored for cases where extremely high recall is crucial, guaranteeing a recall of 1 by comparing
    /// each query with all vectors in the dataset. It only requires the metric type (metric_type) and top-k (limit) as
    /// index building and search parameters.
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/gpu_index.md" />
    /// </remarks>
    GpuBruteForce,

    /// <summary>
    /// ANNOY (Approximate Nearest Neighbors Oh Yeah) is an index that uses a hyperplane to divide a high-dimensional
    /// space into multiple subspaces, and then stores them in a tree structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are just two main parameters needed to tune ANNOY: the number of trees <c>n_trees</c> and the number of
    /// nodes to inspect during searching <c>search_k</c>.
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <c>n_trees</c> is provided during build time and affects the build time and the index size. A larger value will
    /// give more accurate results, but larger indexes.
    /// </item>
    /// <item>
    /// <c>search_k</c> is provided in runtime and affects the search performance. A larger value will give more
    /// accurate results, but will take longer time to return.
    /// </item>
    /// </list>
    /// <para>
    /// If <c>search_k</c> is not provided, it will default to <c>n * n_trees</c> where <c>n</c> is the number of
    /// approximate nearest neighbors. Otherwise, <c>search_k</c> and <c>n_trees</c> are roughly independent, i.e. the
    /// value of <c>n_trees</c> will not affect search time if <c>search_k</c> is held constant and vice versa.
    /// Basically it's recommended to set <c>n_trees</c> as large as possible given the amount of memory you can afford,
    /// and it's recommended to set <c>search_k</c> as large as possible given the time constraints you have for the
    /// queries.
    /// </para>
    /// </remarks>
    Annoy,

    RhnswFlat,
    RhnswPq,
    RhnswSq,
    BinFlat,
    BinIvfFlat,
    AutoIndex,

    /// <summary>
    /// Trie index for scalar fields. A tree-based index for fast prefix matching.
    /// </summary>
    Trie,

    /// <summary>
    /// STL_SORT index for scalar fields. Uses standard library sorting for efficient lookups.
    /// </summary>
    StlSort,

    /// <summary>
    /// Inverted index for scalar fields. Efficient for full-text search and pattern matching on VARCHAR, INT, and FLOAT fields.
    /// </summary>
    Inverted,

    /// <summary>
    /// Sparse inverted index for sparse float vector fields. Available since Milvus v2.4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SPARSE_INVERTED_INDEX uses an inverted index where each dimension maintains a list of vectors
    /// that have a non-zero value at that dimension. This is particularly effective for sparse vectors
    /// with low-dimensional non-zero values.
    /// </para>
    /// <para>
    /// Build parameters: <c>drop_ratio_build</c> (the proportion of small vector values excluded during indexing,
    /// range [0, 1), default 0).
    /// </para>
    /// <para>
    /// Search parameters: <c>drop_ratio_search</c> (the proportion of small vector values excluded during search,
    /// range [0, 1), default 0).
    /// </para>
    /// </remarks>
    SparseInvertedIndex,

    /// <summary>
    /// WAND (Weak AND) algorithm-based sparse index. Available since Milvus v2.4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SPARSE_WAND uses the Weak-AND algorithm to quickly skip a large number of unlikely candidates
    /// during search, enabling faster sparse vector searches.
    /// </para>
    /// <para>
    /// Build parameters: <c>drop_ratio_build</c> (the proportion of small vector values excluded during indexing,
    /// range [0, 1), default 0).
    /// </para>
    /// <para>
    /// Search parameters: <c>drop_ratio_search</c> (the proportion of small vector values excluded during search,
    /// range [0, 1), default 0).
    /// </para>
    /// </remarks>
    [Obsolete("SPARSE_WAND is obsolete. Use SparseInvertedIndex instead.")]
    SparseWand,
}
