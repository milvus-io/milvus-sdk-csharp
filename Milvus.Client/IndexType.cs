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
    /// Build parameters (passed via <c>extraParams</c>):
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <c>inverted_index_algo</c>: The query algorithm. Values: <c>"DAAT_MAXSCORE"</c> (default, best for high k
    /// or many terms), <c>"DAAT_WAND"</c> (best for small k or short queries), <c>"TAAT_NAIVE"</c> (adapts to
    /// collection changes). String values must be quoted in JSON, e.g. <c>"\"DAAT_WAND\""</c>.
    /// </item>
    /// <item>
    /// <c>bm25_k1</c>: Controls term frequency saturation for BM25 scoring. Range [1.2, 2.0].
    /// Only applicable when metric type is <see cref="SimilarityMetricType.Bm25" />.
    /// </item>
    /// <item>
    /// <c>bm25_b</c>: Controls document length normalization for BM25 scoring. Range [0, 1], default 0.75.
    /// Only applicable when metric type is <see cref="SimilarityMetricType.Bm25" />.
    /// </item>
    /// </list>
    /// <para>
    /// Search parameters: <c>drop_ratio_search</c> (the proportion of small vector values excluded during search,
    /// range [0, 1), default 0).
    /// </para>
    /// </remarks>
    SparseInvertedIndex,

    /// <summary>
    /// SPARSE_WAND index for sparse float vector fields. Available since Milvus v2.4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses the Weak-AND (WAND) algorithm, which skips low-impact terms during traversal. This is
    /// typically faster than <see cref="SparseInvertedIndex" /> for short queries or small <c>k</c>,
    /// at the cost of being more sensitive to the score distribution.
    /// </para>
    /// <para>
    /// Build parameter: <c>drop_ratio_build</c> (the proportion of small vector values excluded
    /// during index building, range [0, 1)).
    /// </para>
    /// <para>
    /// Search parameter: <c>drop_ratio_search</c> (the proportion of small vector values excluded
    /// during search, range [0, 1), default 0).
    /// </para>
    /// </remarks>
    SparseWand,

    /// <summary>
    /// RTREE spatial index for geometry fields. Available since Milvus v2.6.
    /// </summary>
    /// <remarks>
    /// Accelerates spatial predicates (<c>st_contains</c>, <c>st_within</c>, <c>st_intersects</c>,
    /// <c>st_dwithin</c>, ...) on <see cref="MilvusDataType.Geometry" /> fields.
    /// </remarks>
    RTree,

    /// <summary>
    /// BITMAP index for scalar fields. Available since Milvus v2.5.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stores a bitmap of the distinct values in the field and answers scalar filters with bitwise
    /// operations. This is most effective for low-cardinality fields (Milvus recommends cardinality below
    /// 500); query performance degrades as cardinality grows.
    /// </para>
    /// <para>
    /// Applies to all scalar fields except <c>JSON</c>, <c>FLOAT</c> and <c>DOUBLE</c>, and to
    /// <see cref="MilvusDataType.Array" /> fields whose element type satisfies the same restriction. There
    /// are no build or search parameters.
    /// </para>
    /// </remarks>
    Bitmap,

    /// <summary>
    /// NGRAM index for VARCHAR (and JSON, via a string-typed path) fields. Available since Milvus v2.6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Splits indexed strings into overlapping substrings of length between <c>min_gram</c> and
    /// <c>max_gram</c> (both required build parameters, with <c>min_gram &lt;= max_gram</c>), accelerating
    /// <c>LIKE</c>/wildcard and regex pattern-matching queries whose literal substrings fall within that
    /// length range.
    /// </para>
    /// <para>
    /// To index a JSON path instead of a VARCHAR field, also set the <c>json_path</c> (e.g.
    /// <c>"json_field[\"body\"]"</c>) and <c>json_cast_type</c> (currently only <c>"varchar"</c> is
    /// supported) build parameters.
    /// </para>
    /// </remarks>
    Ngram,

    /// <summary>
    /// MINHASH_LSH index for binary vectors holding MinHash signatures, used for Jaccard-similarity
    /// near-duplicate detection. Available since Milvus v2.6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires <see cref="SimilarityMetricType.MhJaccard" /> as the metric type. The indexed binary vector
    /// field's dimension must equal <c>mh_element_bit_width</c> multiplied by the number of MinHash
    /// signatures stored per row.
    /// </para>
    /// <para>
    /// Build parameters (passed via <c>extraParams</c>): <c>mh_element_bit_width</c> (bit width of each
    /// hash value; one of 8, 16, 32, 64), <c>mh_lsh_band</c> (number of LSH bands the signature is split
    /// into), <c>mh_lsh_code_in_mem</c> (keep LSH codes in memory rather than memory-mapped, boolean),
    /// <c>with_raw_data</c> (keep the raw MinHash signatures alongside the LSH codes so results can be
    /// refined, boolean, default <c>false</c>), and <c>mh_lsh_bloom_false_positive_prob</c> (bloom-filter
    /// false-positive rate, range [0.001, 0.1]).
    /// </para>
    /// <para>
    /// Search parameters: <c>mh_search_with_jaccard</c> (compute exact Jaccard similarity over the LSH
    /// candidates, boolean), <c>refine_k</c> (candidate multiplier before refinement), and
    /// <c>mh_lsh_batch_search</c> (batch multiple query vectors together, boolean).
    /// </para>
    /// </remarks>
    MinHashLsh,

    /// <summary>
    /// IVF_RABITQ index for float vector fields, combining IVF clustering with RaBitQ binary quantization.
    /// Available since Milvus v2.6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Quantizes each vector down to roughly 1 bit per dimension, giving up to a 32x storage reduction over
    /// an unquantized index while retaining a tunable amount of recall via optional refinement.
    /// </para>
    /// <para>
    /// Build parameters: <c>nlist</c> (number of cluster units, range [1, 65536], default 128),
    /// <c>refine</c> (whether to keep additional data for search-time refinement, boolean, default
    /// <c>false</c>), and, when <c>refine</c> is <c>true</c>, <c>refine_type</c> (the precision used for
    /// refinement data: one of <c>"SQ6"</c>, <c>"SQ8"</c>, <c>"FP16"</c>, <c>"BF16"</c>, <c>"FP32"</c> —
    /// note string values must be quoted in JSON, e.g. <c>"\"SQ8\""</c>).
    /// </para>
    /// <para>
    /// Search parameters: <c>nprobe</c> (number of clusters to search, range [1, nlist]),
    /// <c>rbq_bits_query</c> (query-vector quantization level, 0 to disable or 1-8 for SQ1-SQ8 — note the
    /// word order: knowhere's <c>IvfRaBitQConfig</c> and the official Go SDK both read
    /// <c>rbq_bits_query</c>, not <c>rbq_query_bits</c> as the milvus.io docs page for this index
    /// currently states), and, when <c>refine</c> was enabled at build time, <c>refine_k</c> (refinement
    /// candidate multiplier, &gt;= 1).
    /// </para>
    /// </remarks>
    IvfRabitq,
}
