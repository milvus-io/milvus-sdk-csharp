using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The state of a compaction together with the per-compaction merge plans (source segments -> target segment).
/// </summary>
public sealed class GetCompactionPlansResp
{
    internal GetCompactionPlansResp(CompactionState state, IReadOnlyList<CompactionMergeInfo> mergeInfos)
    {
        State = state;
        MergeInfos = mergeInfos;
    }

    internal static GetCompactionPlansResp FromGrpc(Grpc.GetCompactionPlansResponse response)
        => new(
            (CompactionState)response.State,
            response.MergeInfos.Select(m => new CompactionMergeInfo(m.Sources.ToArray(), m.Target)).ToArray());

    /// <summary>
    /// The state of the compaction.
    /// </summary>
    public CompactionState State { get; }

    /// <summary>
    /// The merge plans for the compaction: each entry maps the source segments onto a target segment.
    /// </summary>
    public IReadOnlyList<CompactionMergeInfo> MergeInfos { get; }
}

/// <summary>
/// A single compaction merge plan: the source segments that are merged into the target segment.
/// </summary>
public sealed class CompactionMergeInfo
{
    internal CompactionMergeInfo(IReadOnlyList<long> sources, long target)
    {
        Sources = sources;
        Target = target;
    }

    /// <summary>
    /// The segment IDs that are merged as part of this plan.
    /// </summary>
    public IReadOnlyList<long> Sources { get; }

    /// <summary>
    /// The target segment ID produced by this merge.
    /// </summary>
    public long Target { get; }
}
