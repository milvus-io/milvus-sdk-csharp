namespace IO.Milvus.ApiSchema;

/// <summary>
/// Indicate build progress of an index.
/// </summary>
public struct IndexBuildProgress
{
    /// <summary>
    /// Construct a index progress struct.
    /// </summary>
    /// <param name="indexedRows">Indexed rows.</param>
    /// <param name="totalRows">Total rows.</param>
    public IndexBuildProgress(long indexedRows, long totalRows)
    {
        IndexedRows = indexedRows;
        TotalRows = totalRows;
    }

    /// <summary>
    /// Indexed rows.
    /// </summary>
    public long IndexedRows { get; }

    /// <summary>
    /// Total rows.
    /// </summary>
    public long TotalRows { get; }

    ///<inheritdoc/>
    public override string ToString()
    {
        return $"Progress: {IndexedRows}/{TotalRows}";
    }
}
