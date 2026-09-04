using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The persistent segment info of a collection.
/// </summary>
public sealed class PersistentSegmentInfo
{
    internal PersistentSegmentInfo(
        string collectionName, long segmentId, long collectionId, long partitionId, long numRows,
        SegmentState state, SegmentLevel level, bool isSorted, long storageVersion)
    {
        CollectionName = collectionName;
        SegmentId = segmentId;
        CollectionId = collectionId;
        PartitionId = partitionId;
        NumRows = numRows;
        State = state;
        Level = level;
        IsSorted = isSorted;
        StorageVersion = storageVersion;
    }

    internal static PersistentSegmentInfo FromGrpc(Grpc.PersistentSegmentInfo info, string collectionName = "")
        => new(
            collectionName, info.SegmentID, info.CollectionID, info.PartitionID, info.NumRows,
            (SegmentState)info.State, (SegmentLevel)info.Level, info.IsSorted, info.StorageVersion);

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
    /// The number of rows stored in the segment.
    /// </summary>
    public long NumRows { get; }

    /// <summary>
    /// The state of the segment.
    /// </summary>
    public SegmentState State { get; }

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
