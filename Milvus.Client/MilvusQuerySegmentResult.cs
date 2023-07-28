namespace Milvus.Client;

/// <summary>
/// Milvus query segment result.
/// </summary>
public sealed class MilvusQuerySegmentInfoResult
{
    internal MilvusQuerySegmentInfoResult(
        long collectionId,
        string indexName,
        long indexId,
        long memSize,
        long nodeId,
        long numRows,
        long partitionId,
        long segmentId,
        MilvusSegmentState state)
    {
        CollectionId = collectionId;
        IndexName = indexName;
        IndexId = indexId;
        MemSize = memSize;
        NodeId = nodeId;
        NumRows = numRows;
        PartitionId = partitionId;
        SegmentId = segmentId;
        State = state;
    }

    /// <summary>
    /// Collection id.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// Index name.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// Index id.
    /// </summary>
    public long IndexId { get; }

    /// <summary>
    /// Memory size.
    /// </summary>
    public long MemSize { get; }

    /// <summary>
    /// Node id.
    /// </summary>
    public long NodeId { get; }

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
    /// State.
    /// </summary>
    public MilvusSegmentState State { get; }
}
