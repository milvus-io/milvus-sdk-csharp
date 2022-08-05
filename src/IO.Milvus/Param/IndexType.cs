namespace IO.Milvus.Param
{
    /// <summary>
    ///  * Represents the available index types.
    /// For more information: @see<a href="https://milvus.io/docs/v2.0.0/index_selection.md"> Index Types</a>
    /// </summary>
    public enum IndexType
    {
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
    }
}
