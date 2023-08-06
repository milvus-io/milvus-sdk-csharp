namespace Milvus.Client;

/// <summary>
/// Milvus persistent segment info
/// </summary>
public sealed class PersistentSegmentInfo
{
    internal PersistentSegmentInfo(
        long collectionId, long partitionId, long segmentId, long numRows, Grpc.SegmentState state)
    {
        CollectionId = collectionId;
        PartitionId = partitionId;
        SegmentId = segmentId;
        NumRows = numRows;
        State = (SegmentState)state;
    }

    /// <summary>
    /// Collection id.
    /// </summary>
    public long CollectionId { get; set; }

    /// <summary>
    /// Number of rows.
    /// </summary>
    public long NumRows { get; set; }

    /// <summary>
    /// Partition id.
    /// </summary>
    public long PartitionId { get; set; }

    /// <summary>
    /// Segment id.
    /// </summary>
    public long SegmentId { get; set; }

    /// <summary>
    /// State
    /// </summary>
    public SegmentState State { get; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
        => $"MilvusPersistentSegmentInfo {{{nameof(State)}: {State}, {nameof(SegmentId)}: {SegmentId}, {nameof(CollectionId)}: {CollectionId}, {nameof(PartitionId)}: {PartitionId}, {nameof(NumRows)}: {NumRows}}}";
}
