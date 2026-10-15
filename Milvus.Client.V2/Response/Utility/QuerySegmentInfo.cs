using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The query-segment info of a loaded collection.
/// </summary>
public sealed class QuerySegmentInfo
{
    internal QuerySegmentInfo(
        string collectionName, long segmentId, long collectionId, long partitionId, long memSize, long numRows,
        string indexName, long indexId, SegmentState state, IReadOnlyList<long> nodeIds, SegmentLevel level,
        bool isSorted, long storageVersion)
    {
        CollectionName = collectionName;
        SegmentId = segmentId;
        CollectionId = collectionId;
        PartitionId = partitionId;
        MemSize = memSize;
        NumRows = numRows;
        IndexName = indexName;
        IndexId = indexId;
        State = state;
        NodeIds = nodeIds;
        Level = level;
        IsSorted = isSorted;
        StorageVersion = storageVersion;
    }

    internal static QuerySegmentInfo FromGrpc(Grpc.QuerySegmentInfo info, string collectionName = "")
        => new(
            collectionName, info.SegmentID, info.CollectionID, info.PartitionID, info.MemSize, info.NumRows,
            info.IndexName, info.IndexID, (SegmentState)info.State, info.NodeIds.ToList(), (SegmentLevel)info.Level,
            info.IsSorted, info.StorageVersion);

    /// <summary>
    /// The name of the collection that owns the segment (from the request; the server reports only the id).
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The segment id.
    /// </summary>
    public long SegmentId { get; }

    /// <summary>
    /// The id of the collection that owns the segment.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// The id of the partition that owns the segment.
    /// </summary>
    public long PartitionId { get; }

    /// <summary>
    /// The memory size of the segment in bytes.
    /// </summary>
    public long MemSize { get; }

    /// <summary>
    /// The number of rows stored in the segment.
    /// </summary>
    public long NumRows { get; }

    /// <summary>
    /// The name of the index built on the segment.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// The id of the index built on the segment.
    /// </summary>
    public long IndexId { get; }

    /// <summary>
    /// The state of the segment.
    /// </summary>
    public SegmentState State { get; }

    /// <summary>
    /// The query nodes serving the segment.
    /// </summary>
    public IReadOnlyList<long> NodeIds { get; }

    /// <summary>
    /// The level of the segment (sealed, growing, etc.).
    /// </summary>
    public SegmentLevel Level { get; }

    /// <summary>
    /// Whether the segment is sorted.
    /// </summary>
    public bool IsSorted { get; }

    /// <summary>
    /// The storage format version of the segment.
    /// </summary>
    public long StorageVersion { get; }
}
