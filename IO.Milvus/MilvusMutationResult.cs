using IO.Milvus.Utils;

namespace IO.Milvus;

/// <summary>
/// Mutation result wrapper
/// </summary>
public sealed class MilvusMutationResult
{
    private MilvusMutationResult(
        long insertCount,
        long deletedCount,
        long upsertCount,
        bool acknowledged,
        IList<uint> successIndex,
        IList<uint> errorIndex,
        DateTime dateTime,
        MilvusIds? ids,
        Grpc.MutationResult? mutationResult = null)
    {
        InsertCount = insertCount;
        DeleteCount = deletedCount;
        UpsertCount = upsertCount;
        Acknowledged = acknowledged;
        SuccessIndex = successIndex;
        ErrorIndex = errorIndex;
        Timestamp = dateTime;
        Ids = ids;
        MutationResult = mutationResult;
    }

    internal static MilvusMutationResult From(Grpc.MutationResult mutationResult)
        => new(
            mutationResult.InsertCnt,
            mutationResult.DeleteCnt,
            mutationResult.UpsertCnt,
            mutationResult.Acknowledged,
            mutationResult.SuccIndex.ToList(),
            mutationResult.ErrIndex.ToList(),
            TimestampUtils.GetTimeFromTimestamp((long)mutationResult.Timestamp),
            MilvusIds.FromGrpc(mutationResult.IDs),
            mutationResult);

    /// <summary>
    /// Source mutation result from grpc response.
    /// </summary>
    public Grpc.MutationResult? MutationResult { get; }

    /// <summary>
    /// Acknowledged.
    /// </summary>

    public bool Acknowledged { get; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Insert count.
    /// </summary>
    public long InsertCount { get; }

    /// <summary>
    /// Error count.
    /// </summary>
    public long DeleteCount { get; }

    /// <summary>
    /// Upsert count.
    /// </summary>
    public long UpsertCount { get; }

    /// <summary>
    /// Success index.
    /// </summary>
    public IList<uint> SuccessIndex { get; }

    /// <summary>
    /// Error index.
    /// </summary>
    public IList<uint> ErrorIndex { get; }

    /// <summary>
    /// Ids
    /// </summary>
    public MilvusIds? Ids { get; set; } // TODO NULLABILITY: Confirm nullability
}
