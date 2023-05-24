namespace IO.Milvus;

/// <summary>
/// Milvus persistent segment info
/// </summary>
public class MilvusPersistentSegmentInfo
{
    /// <summary>
    /// Collection id.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// Number of rows.
    /// </summary>
    public long NumRows { get; }

    /// <summary>
    /// Partition id.
    /// </summary>
    public long PartitionId { get; }

    /// <summary>
    /// Segment id.
    /// </summary>
    public long SegmentId { get; }

    /// <summary>
    /// State
    /// </summary>
    public int State { get; }
}
