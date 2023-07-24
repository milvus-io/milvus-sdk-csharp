namespace Milvus.Client;

/// <summary>
/// Milvus segment state.
/// </summary>
public enum MilvusSegmentState
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Not exist.
    /// </summary>
    NotExist = 1,

    /// <summary>
    /// Growing.
    /// </summary>
    Growing = 2,

    /// <summary>
    /// Sealed.
    /// </summary>
    Sealed = 3,

    /// <summary>
    /// Flushed.
    /// </summary>
    Flushed = 4,

    /// <summary>
    /// Flushing.
    /// </summary>
    Flushing = 5,

    /// <summary>
    /// Dropped.
    /// </summary>
    Dropped = 6,

    /// <summary>
    /// Importing.
    /// </summary>
    Importing = 7,
}
