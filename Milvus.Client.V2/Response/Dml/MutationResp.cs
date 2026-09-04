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
        long insertCount, long deleteCount, long upsertCount, ulong timestamp)
    {
        LongIds = longIds;
        StringIds = stringIds;
        InsertCount = insertCount;
        DeleteCount = deleteCount;
        UpsertCount = upsertCount;
        Timestamp = timestamp;
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
            response.Timestamp);
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
}
