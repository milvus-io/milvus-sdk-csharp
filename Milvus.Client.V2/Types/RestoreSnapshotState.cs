namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the state of a restore snapshot job.
/// </summary>
public enum RestoreSnapshotState
{
    /// <summary>
    /// The restore state is undefined.
    /// </summary>
    None = 0,

    /// <summary>
    /// The restore job has been created and is waiting to start.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The restore job is currently executing.
    /// </summary>
    Executing = 2,

    /// <summary>
    /// The restore job has completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The restore job has failed.
    /// </summary>
    Failed = 4
}
