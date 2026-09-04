namespace Milvus.Client.V2.Types;

/// <summary>
/// The state of an index build.
/// </summary>
public enum IndexState
{
    /// <summary>
    /// The index does not exist.
    /// </summary>
    None = 0,

    /// <summary>
    /// The index build has not been issued.
    /// </summary>
    Unissued = 1,

    /// <summary>
    /// The index build is in progress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// The index build has finished.
    /// </summary>
    Finished = 3,

    /// <summary>
    /// The index build failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The index build is scheduled for a retry.
    /// </summary>
    Retry = 5
}
