namespace Milvus.Client;

/// <summary>
/// Milvus compaction plans.
/// </summary>
public sealed class CompactionPlans
{
    internal CompactionPlans(IReadOnlyList<MilvusCompactionPlan> plans, CompactionState state)
    {
        Plans = plans;
        State = state;
    }

    /// <summary>
    /// Merge infos.
    /// </summary>
    public IReadOnlyList<MilvusCompactionPlan> Plans { get; }

    /// <summary>
    /// State.
    /// </summary>
    public CompactionState State { get; }
}

/// <summary>
/// Milvus compaction plan.
/// </summary>
public sealed class MilvusCompactionPlan
{
    /// <summary>
    /// Sources
    /// </summary>
    public required IReadOnlyList<long> Sources { get; set; }

    /// <summary>
    /// Target
    /// </summary>
    public long Target { get; set; }
}
