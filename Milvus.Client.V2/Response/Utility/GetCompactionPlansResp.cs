using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The state of a compaction together with the per-compaction merge plans (source segments -> target segment).
/// Returned by <c>GetCompactionPlansAsync</c> and <c>ListCompactionTasksAsync</c>.
/// </summary>
public sealed class GetCompactionPlansResp
{
    internal GetCompactionPlansResp(
        long? compactionId, string? collectionName, CompactionState state, IReadOnlyList<CompactionMergeInfo> mergeInfos)
    {
        CompactionId = compactionId;
        CollectionName = collectionName;
        State = state;
        MergeInfos = mergeInfos;
    }

    internal static GetCompactionPlansResp FromGrpc(
        long? compactionId, string? collectionName, Grpc.GetCompactionPlansResponse response)
        => new(
            compactionId,
            collectionName,
            (CompactionState)response.State,
            response.MergeInfos.Select(CompactionMergeInfo.FromGrpc).ToArray());

    /// <summary>
    /// The id of the compaction whose plans are reported, or <c>null</c> when the tasks were selected
    /// by collection name (see <c>ListCompactionTasksAsync</c>).
    /// </summary>
    public long? CompactionId { get; }

    /// <summary>
    /// The name of the collection the reported compaction tasks belong to, or <c>null</c> when the plans
    /// were selected by compaction id (see <c>GetCompactionPlansAsync</c>).
    /// </summary>
    public string? CollectionName { get; }

    /// <summary>
    /// The aggregate state of the compaction. When tasks are selected by collection name
    /// (<c>ListCompactionTasksAsync</c>), the server collapses this to <see cref="CompactionState.Executing" />
    /// or <see cref="CompactionState.Completed" /> based only on the non-terminal task count: a retained task
    /// that failed or timed out does not move the aggregate state away from
    /// <see cref="CompactionState.Completed" />. Read each plan's <see cref="CompactionMergeInfo.State" /> /
    /// <see cref="CompactionMergeInfo.FailureReason" /> for per-task failures.
    /// </summary>
    public CompactionState State { get; }

    /// <summary>
    /// The merge plans for the compaction: each entry maps the source segments onto a target segment.
    /// </summary>
    public IReadOnlyList<CompactionMergeInfo> MergeInfos { get; }
}

/// <summary>
/// A single compaction merge plan: the source segments that are merged into the target segment, together
/// with the per-task metadata reported by the server (plan id, trigger id, type, state, failure reason, ...).
/// </summary>
public sealed class CompactionMergeInfo
{
    internal CompactionMergeInfo(
        IReadOnlyList<long> sources,
        long target,
        long planId,
        long triggerId,
        long collectionId,
        long partitionId,
        string channel,
        CompactionType compactionType,
        CompactionTaskState state,
        string failureReason,
        IReadOnlyList<long> targets)
    {
        Sources = sources;
        Target = target;
        PlanId = planId;
        TriggerId = triggerId;
        CollectionId = collectionId;
        PartitionId = partitionId;
        Channel = channel;
        CompactionType = compactionType;
        State = state;
        FailureReason = failureReason;
        Targets = targets;
    }

    internal static CompactionMergeInfo FromGrpc(Grpc.CompactionMergeInfo info)
        => new(
            info.Sources.ToArray(),
            info.Target,
            info.PlanId,
            info.TriggerId,
            info.CollectionId,
            info.PartitionId,
            info.Channel,
            (CompactionType)info.Type,
            (CompactionTaskState)info.State,
            info.FailureReason,
            info.Targets.ToArray());

    /// <summary>
    /// The segment IDs that are merged as part of this plan.
    /// </summary>
    public IReadOnlyList<long> Sources { get; }

    /// <summary>
    /// The target segment ID produced by this merge.
    /// </summary>
    public long Target { get; }

    /// <summary>
    /// The complete set of target segments produced by this compaction, which may be larger than the
    /// single legacy <see cref="Target" />.
    /// </summary>
    public IReadOnlyList<long> Targets { get; }

    /// <summary>
    /// The server-side compaction task identifier.
    /// </summary>
    public long PlanId { get; }

    /// <summary>
    /// The id of the compaction that triggered this plan.
    /// </summary>
    public long TriggerId { get; }

    /// <summary>
    /// The id of the collection being compacted.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// The id of the partition being compacted.
    /// </summary>
    public long PartitionId { get; }

    /// <summary>
    /// The channel on which the compaction was scheduled.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// The type of the compaction task.
    /// </summary>
    public CompactionType CompactionType { get; }

    /// <summary>
    /// The state of the compaction task.
    /// </summary>
    public CompactionTaskState State { get; }

    /// <summary>
    /// The failure reason of the compaction task, empty when it did not fail.
    /// </summary>
    public string FailureReason { get; }
}
