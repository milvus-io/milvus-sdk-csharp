namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the state of a compaction task.
/// </summary>
public enum CompactionState
{
    /// <summary>
    /// The compaction state is undefined.
    /// </summary>
    UndefinedState = 0,

    /// <summary>
    /// The compaction is currently executing.
    /// </summary>
    Executing = 1,

    /// <summary>
    /// The compaction has completed.
    /// </summary>
    Completed = 2
}
