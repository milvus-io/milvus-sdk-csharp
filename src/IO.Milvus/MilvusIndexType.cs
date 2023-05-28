namespace IO.Milvus;

/// <summary>
/// Milvus index type.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <listheader>Usage:</listheader>
/// <item>For zilliz cloud:</item>
/// <item><see cref="MilvusIndexType.AUTOINDEX"/></item>
/// <item>For For floating point vectors.:</item> 
/// <item><see cref="MilvusIndexType.FLAT"/></item>
/// <item><see cref="MilvusIndexType.IVF_FLAT"/></item>
/// <item><see cref="MilvusIndexType.IVF_SQ8"/></item>
/// <item><see cref="MilvusIndexType.IVF_PQ"/></item>
/// <item><see cref="MilvusIndexType.HNSW"/></item>
/// <item><see cref="MilvusIndexType.ANNOY"/></item>
/// <item>For binary vectors:</item>
/// <item><see cref="MilvusIndexType.BIN_FLAT"/></item>
/// <item><see cref="MilvusIndexType.BIN_IVF_FLAT"/></item>
/// </list>
/// For more information, please refer to <see href="https://milvus.io/docs/v2.0.0/index.md#Index"/>
/// </remarks>
public enum MilvusIndexType
{
    /// <summary>
    /// Zilliz cloud request a auto index type.
    /// </summary>
    AUTOINDEX,

    /// <summary>
    /// Invalid.
    /// </summary>
    INVALID,

    /// <summary>
    /// Flat.
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    FLAT,

    /// <summary>
    /// IVF_FLAT.
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    IVF_FLAT,

    /// <summary>
    /// IVF_PQ
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    IVF_PQ,

    /// <summary>
    /// IVF_SQ8
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    IVF_SQ8,

    /// <summary>
    /// IVF_HNSW
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    IVF_HNSW,

    /// <summary>
    /// HNSW
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    HNSW,

    /// <summary>
    /// RHNSW_FLAT
    /// </summary>
    RHNSW_FLAT,

    /// <summary>
    /// RHNSW_PQ
    /// </summary>
    RHNSW_PQ,

    /// <summary>
    /// RHNSW_SQ
    /// </summary>
    RHNSW_SQ,

    /// <summary>
    /// BIN_FLAT
    /// </summary>
    ANNOY,

    /// <summary>
    /// Only supported for binary vectors
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    BIN_FLAT,

    /// <summary>
    /// BIN_IVF_FLAT
    /// </summary>
    /// <remarks>
    /// For For floating point vectors.
    /// </remarks>
    BIN_IVF_FLAT,

    /// <summary>
    /// TRIE
    /// </summary>
    TRIE,
}