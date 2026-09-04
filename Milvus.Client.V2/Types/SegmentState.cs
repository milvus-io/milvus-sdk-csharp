namespace Milvus.Client.V2.Types;

/// <summary>
/// The state of a segment on a query node (mirrors <c>common.SegmentState</c>).
/// </summary>
public enum SegmentState
{
    /// <summary>Default value, not set.</summary>
    None = 0,

    /// <summary>The segment no longer exists.</summary>
    NotExist = 1,

    /// <summary>The segment is being built or loaded.</summary>
    Growing = 2,

    /// <summary>The segment is sealed and searchable.</summary>
    Sealed = 3,

    /// <summary>The segment has been flushed to storage.</summary>
    Flushed = 4,

    /// <summary>The segment is currently being flushed.</summary>
    Flushing = 5,

    /// <summary>The segment has been dropped.</summary>
    Dropped = 6,

    /// <summary>The segment is being imported.</summary>
    Importing = 7
}

/// <summary>
/// The level of a segment (mirrors <c>common.SegmentLevel</c>).
/// </summary>
public enum SegmentLevel
{
    /// <summary>Legacy level, zero value.</summary>
    Legacy = 0,

    /// <summary>L0 segment: contains delta data for the current channel.</summary>
    L0 = 1,

    /// <summary>L1 segment: normal segment, with no extra compaction attribute.</summary>
    L1 = 2,

    /// <summary>L2 segment: segment with extra data distribution info.</summary>
    L2 = 3
}
