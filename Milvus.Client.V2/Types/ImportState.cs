namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the state of an import task.
/// </summary>
public enum ImportState
{
    /// <summary>
    /// The import task is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The import task has failed.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// The import task has started.
    /// </summary>
    Started = 2,

    /// <summary>
    /// The imported data has been persisted.
    /// </summary>
    Persisted = 5,

    /// <summary>
    /// The import task has completed.
    /// </summary>
    Completed = 6,

    /// <summary>
    /// The import task failed and the imported data has been cleaned up.
    /// </summary>
    FailedAndCleaned = 7,

    /// <summary>
    /// The imported data has been flushed.
    /// </summary>
    Flushed = 8
}
