namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the state of a refresh-external-collection job.
/// </summary>
public enum RefreshExternalCollectionState
{
    /// <summary>
    /// The refresh job has been created but has not started running.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The refresh job is currently executing.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// The refresh job has completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The refresh job has failed.
    /// </summary>
    Failed = 3
}
