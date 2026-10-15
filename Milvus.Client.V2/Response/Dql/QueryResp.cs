using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Dql;

/// <summary>
/// Represents the result of a query operation.
/// </summary>
public sealed class QueryResp
{
    private QueryResp(
        string collectionName, IReadOnlyList<FieldData> fieldsData, ulong sessionTs, string? primaryKeyName,
        long cost, long scannedRemoteBytes, long scannedTotalBytes, float? cacheHitRatio)
    {
        CollectionName = collectionName;
        FieldsData = fieldsData;
        SessionTs = sessionTs;
        PrimaryKeyName = primaryKeyName;
        Cost = cost;
        ScannedRemoteBytes = scannedRemoteBytes;
        ScannedTotalBytes = scannedTotalBytes;
        CacheHitRatio = cacheHitRatio;
    }

    internal static QueryResp FromGrpc(Grpc.QueryResults response)
        => new(
            response.CollectionName,
            DqlConversions.ProcessReturnedFieldData(response.FieldsData),
            response.SessionTs,
            string.IsNullOrEmpty(response.PrimaryFieldName) ? null : response.PrimaryFieldName,
            DqlConversions.GetReportValue(response.Status),
            DqlConversions.GetExtraInfoLong(response.Status, "scanned_remote_bytes"),
            DqlConversions.GetExtraInfoLong(response.Status, "scanned_total_bytes"),
            DqlConversions.GetExtraInfoFloat(response.Status, "cache_hit_ratio"));

    /// <summary>
    /// The name of the queried collection.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The returned fields data.
    /// </summary>
    public IReadOnlyList<FieldData> FieldsData { get; }

    /// <summary>
    /// The name of the primary-key field of the queried collection, as reported by the server, so callers can
    /// identify the pk column in <see cref="FieldsData" /> without re-describing the collection.
    /// </summary>
    public string? PrimaryKeyName { get; }

    /// <summary>
    /// The server-side timestamp at which the query was executed, used for session-like operations such as
    /// iterators (read-your-writes consistency).
    /// </summary>
    public ulong SessionTs { get; }

    /// <summary>
    /// The cost of the query in milliseconds, as reported by the server's <c>report_value</c> extra info,
    /// or 0 when the server does not report a cost. Mirrors the Java SDK's <c>QueryResp</c> metrics.
    /// </summary>
    public long Cost { get; }

    /// <summary>
    /// The number of bytes scanned from remote storage, or 0 when the server does not report it.
    /// </summary>
    public long ScannedRemoteBytes { get; }

    /// <summary>
    /// The total number of bytes scanned, or 0 when the server does not report it.
    /// </summary>
    public long ScannedTotalBytes { get; }

    /// <summary>
    /// The cache hit ratio, or <c>null</c> when the server does not report it.
    /// </summary>
    public float? CacheHitRatio { get; }

    /// <summary>
    /// The number of rows in the result, 0 when no field data was returned.
    /// </summary>
    public int RowCount => FieldsData.Count == 0 ? 0 : FieldsData[0].RowCount;

    /// <summary>
    /// Materializes the row at <paramref name="rowIndex" /> into a row dictionary mapping field name to
    /// value, the same shape the row-based insert/upsert accepts.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index within the result.</param>
    /// <returns>A dictionary mapping each returned field name to its value for the row.</returns>
    public IReadOnlyDictionary<string, object?> GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex,
                $"Must be in [0, {RowCount}).");
        }

        var row = new Dictionary<string, object?>(FieldsData.Count, StringComparer.Ordinal);
        foreach (FieldData field in FieldsData)
        {
            row[field.FieldName] = field.GetValueAsObject(rowIndex);
        }

        return row;
    }
}
