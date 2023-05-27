namespace IO.Milvus;

/// <summary>
/// Milvus index type.
/// </summary>
public enum MilvusIndexType
{
    AUTOINDEX,

    INVALID,

    FLAT,

    IVF_FLAT,

    IVF_PQ,

    IVF_SQ8,

    IVF_HNSW,

    HNSW,

    RHNSW_FLAT,

    RHNSW_PQ,

    RHNSW_SQ,

    ANNOY,

    /// <summary>
    /// Only supported for binary vectors
    /// </summary>
    BIN_FLAT,

    BIN_IVF_FLAT,
    TRIE,
}