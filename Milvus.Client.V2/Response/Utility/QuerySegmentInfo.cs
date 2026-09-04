#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class QuerySegmentInfo
{
    internal QuerySegmentInfo(long segmentId, long collectionId, long partitionId, long memSize, long numRows, string indexName)
    {
        SegmentId = segmentId;
        CollectionId = collectionId;
        PartitionId = partitionId;
        MemSize = memSize;
        NumRows = numRows;
        IndexName = indexName;
    }
    internal static QuerySegmentInfo FromGrpc(Grpc.QuerySegmentInfo info)
        => new(info.SegmentID, info.CollectionID, info.PartitionID, info.MemSize, info.NumRows, info.IndexName);
    public long SegmentId { get; }
    public long CollectionId { get; }
    public long PartitionId { get; }
    public long MemSize { get; }
    public long NumRows { get; }
    public string IndexName { get; }
}
