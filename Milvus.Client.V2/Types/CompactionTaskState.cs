namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the state of a single compaction task, as reported per plan by the
/// <c>GetCompactionPlansAsync</c> and <c>ListCompactionTasksAsync</c> APIs. This is distinct from
/// the aggregate <see cref="CompactionState" /> of a compaction.
/// </summary>
public enum CompactionTaskState
{
    /// <summary>
    /// The task state is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The task is being executed.
    /// </summary>
    Executing = 1,

    /// <summary>
    /// The task is waiting in the pipeline.
    /// </summary>
    Pipelining = 2,

    /// <summary>
    /// The task has completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The task has failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The task has timed out.
    /// </summary>
    Timeout = 5,

    /// <summary>
    /// The task is analyzing.
    /// </summary>
    Analyzing = 6,

    /// <summary>
    /// The task is indexing.
    /// </summary>
    Indexing = 7,

    /// <summary>
    /// The task has been cleaned up.
    /// </summary>
    Cleaned = 8,

    /// <summary>
    /// The task metadata has been saved.
    /// </summary>
    MetaSaved = 9,

    /// <summary>
    /// The task is collecting statistics.
    /// </summary>
    Statistic = 10
}
