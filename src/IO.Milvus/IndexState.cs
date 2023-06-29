namespace IO.Milvus;

/// <summary>
/// Index state.
/// </summary>
public enum IndexState
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Unissued.
    /// </summary>
    Unissued = 1,

    /// <summary>
    /// InProgress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Finished.
    /// </summary>
    Finished = 3,

    /// <summary>
    /// Failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Retry.
    /// </summary>
    Retry = 5,
}
