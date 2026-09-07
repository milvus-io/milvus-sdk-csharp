#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class PersistentSegmentInfo
{
    internal PersistentSegmentInfo(long segmentId, long collectionId, long numRows)
    {
        SegmentId = segmentId;
        CollectionId = collectionId;
        NumRows = numRows;
    }
    internal static PersistentSegmentInfo FromGrpc(Grpc.PersistentSegmentInfo info)
        => new(info.SegmentID, info.CollectionID, info.NumRows);
    public long SegmentId { get; }
    public long CollectionId { get; }
    public long NumRows { get; }
}
