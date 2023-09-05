namespace Milvus.Client;

/// <summary>
/// Contains information about an index in Milvus; returned from <see cref="MilvusCollection.DescribeIndexAsync" />.
/// </summary>
public sealed class MilvusIndexInfo
{
    internal MilvusIndexInfo(
        string fieldName,
        string indexName,
        long indexId,
        IndexState state,
        long indexedRows,
        long totalRows,
        long pendingIndexRows,
        string? indexStateFailReason,
        IReadOnlyDictionary<string, string> @params)
    {
        FieldName = fieldName;
        IndexName = indexName;
        IndexId = indexId;
        State = state;
        IndexedRows = indexedRows;
        TotalRows = totalRows;
        PendingIndexRows = pendingIndexRows;
        IndexStateFailReason = indexStateFailReason;
        Params = @params;
    }

    /// <summary>
    /// Field name.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Index name.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// Index id.
    /// </summary>
    public long IndexId { get; }

    /// <summary>
    /// The state of the index.
    /// </summary>
    public IndexState State { get; }

    /// <summary>
    /// The number of rows indexed by this index;
    /// </summary>
    public long IndexedRows { get; }

    /// <summary>
    /// The total rows in the collection.
    /// </summary>
    public long TotalRows { get; }

    /// <summary>
    /// The number of pending rows in the index.
    /// </summary>
    public long PendingIndexRows { get; }

    /// <summary>
    /// If creation of the index failed, contains the reason for the failure.
    /// </summary>
    public string? IndexStateFailReason { get; }

    /// <summary>
    /// Contains index_type, metric_type, params.
    /// </summary>
    public IReadOnlyDictionary<string, string> Params { get; }

    /// <summary>
    /// Get string data of <see cref="MilvusIndexInfo"/>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
        => $"MilvusIndex: {{{nameof(FieldName)}: {FieldName}, {nameof(IndexName)}: {IndexName}, {nameof(IndexId)}: {IndexId}}}";
}
