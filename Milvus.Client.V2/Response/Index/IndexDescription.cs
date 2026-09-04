using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Index;

/// <summary>
/// Describes an index on a field of a collection.
/// </summary>
public sealed class IndexDescription
{
    internal IndexDescription(
        string indexName, long indexId, string fieldName, IndexState state,
        long indexedRows, long totalRows, long pendingIndexRows, string? indexStateFailReason,
        IReadOnlyDictionary<string, string> parameters)
    {
        IndexName = indexName;
        IndexId = indexId;
        FieldName = fieldName;
        State = state;
        IndexedRows = indexedRows;
        TotalRows = totalRows;
        PendingIndexRows = pendingIndexRows;
        IndexStateFailReason = indexStateFailReason;
        Parameters = parameters;
    }

    internal static IndexDescription FromGrpc(Grpc.IndexDescription grpc)
    {
        var parameters = new Dictionary<string, string>();
        foreach (Grpc.KeyValuePair kv in grpc.Params)
        {
            parameters[kv.Key] = kv.Value;
        }

        return new IndexDescription(
            grpc.IndexName, grpc.IndexID, grpc.FieldName, (IndexState)grpc.State,
            grpc.IndexedRows, grpc.TotalRows, grpc.PendingIndexRows,
            string.IsNullOrEmpty(grpc.IndexStateFailReason) ? null : grpc.IndexStateFailReason,
            parameters);
    }

    /// <summary>
    /// The index name.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// The index id.
    /// </summary>
    public long IndexId { get; }

    /// <summary>
    /// The field name the index is built on.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The index build state.
    /// </summary>
    public IndexState State { get; }

    /// <summary>
    /// The number of rows indexed so far.
    /// </summary>
    public long IndexedRows { get; }

    /// <summary>
    /// The total number of rows to index.
    /// </summary>
    public long TotalRows { get; }

    /// <summary>
    /// The number of rows pending index build.
    /// </summary>
    public long PendingIndexRows { get; }

    /// <summary>
    /// The failure reason when the index build failed, or <c>null</c>.
    /// </summary>
    public string? IndexStateFailReason { get; }

    /// <summary>
    /// The index parameters (e.g. <c>index_type</c>, <c>metric_type</c>, <c>nlist</c>).
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }
}
