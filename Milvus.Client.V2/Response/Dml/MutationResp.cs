using Milvus.Client.V2.Utils;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Dml;

/// <summary>
/// Represents the result of an insert, upsert or delete operation.
/// </summary>
public sealed class MutationResp
{
    private MutationResp(
        IReadOnlyList<long>? longIds, IReadOnlyList<string>? stringIds,
        long insertCount, long deleteCount, long upsertCount, ulong timestamp,
        IReadOnlyList<long>? successIndex, IReadOnlyList<long>? errorIndex, long cost, bool acknowledged)
    {
        LongIds = longIds;
        StringIds = stringIds;
        InsertCount = insertCount;
        DeleteCount = deleteCount;
        UpsertCount = upsertCount;
        Timestamp = timestamp;
        SuccessIndex = successIndex;
        ErrorIndex = errorIndex;
        Cost = cost;
        Acknowledged = acknowledged;
    }

    internal static MutationResp FromGrpc(Grpc.MutationResult response)
    {
        IReadOnlyList<long>? longIds = null;
        IReadOnlyList<string>? stringIds = null;

        if (response.IDs?.IdFieldCase == Grpc.IDs.IdFieldOneofCase.IntId)
        {
            longIds = response.IDs.IntId.Data.ToList();
        }
        else if (response.IDs?.IdFieldCase == Grpc.IDs.IdFieldOneofCase.StrId)
        {
            stringIds = response.IDs.StrId.Data.ToList();
        }

        return new MutationResp(longIds, stringIds, response.InsertCnt, response.DeleteCnt, response.UpsertCnt,
            response.Timestamp,
            response.SuccIndex.Count > 0 ? response.SuccIndex.Select(x => (long)x).ToList() : null,
            response.ErrIndex.Count > 0 ? response.ErrIndex.Select(x => (long)x).ToList() : null,
            DqlConversions.GetReportValue(response.Status),
            response.Acknowledged);
    }

    /// <summary>
    /// The ids of the mutated rows when the primary key is an integer, or <c>null</c> for string keys.
    /// </summary>
    public IReadOnlyList<long>? LongIds { get; }

    /// <summary>
    /// The ids of the mutated rows when the primary key is a string, or <c>null</c> for integer keys.
    /// </summary>
    public IReadOnlyList<string>? StringIds { get; }

    /// <summary>
    /// The number of inserted rows.
    /// </summary>
    public long InsertCount { get; }

    /// <summary>
    /// The number of deleted rows.
    /// </summary>
    public long DeleteCount { get; }

    /// <summary>
    /// The number of upserted rows.
    /// </summary>
    public long UpsertCount { get; }

    /// <summary>
    /// The hybrid timestamp of the mutation, used by the <see cref="CollectionTsCache" /> for Session consistency.
    /// </summary>
    public ulong Timestamp { get; }

    /// <summary>
    /// The zero-based indexes of the rows that were successfully written, or <c>null</c> when the server
    /// reports no partial-failure information. For a fully successful insert/upsert this is typically
    /// <c>[0..rowCount-1]</c>.
    /// </summary>
    public IReadOnlyList<long>? SuccessIndex { get; }

    /// <summary>
    /// The zero-based indexes of the rows that failed, or <c>null</c> when the server reports no
    /// partial-failure information. An empty-but-non-null list means all rows succeeded.
    /// </summary>
    public IReadOnlyList<long>? ErrorIndex { get; }

    /// <summary>
    /// The cost of the mutation in milliseconds, as reported by the server's <c>report_value</c> extra
    /// info, or 0 when the server does not report a cost.
    /// </summary>
    public long Cost { get; }

    /// <summary>
    /// Whether the mutation was acknowledged by the server, matching the V1 SDK's exposure of the proto
    /// <c>acknowledged</c> field. The current server does not populate the flag.
    /// </summary>
    public bool Acknowledged { get; }
}
