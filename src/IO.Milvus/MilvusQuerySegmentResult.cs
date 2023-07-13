using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus query segment result.
/// </summary>
public sealed class MilvusQuerySegmentInfoResult
{
    /// <summary>
    /// Collection id.
    /// </summary>
    public long CollectionId { get; set; }

    /// <summary>
    /// Index name.
    /// </summary>
    public required string IndexName { get; set; }

    /// <summary>
    /// Index id.
    /// </summary>
    public long IndexId { get; set; }

    /// <summary>
    /// Memory size.
    /// </summary>
    public long MemSize { get; set; }

    /// <summary>
    /// Node id.
    /// </summary>
    public long NodeId { get; set; }

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
    /// State.
    /// </summary>
    public MilvusSegmentState State { get; set; }

    internal static IEnumerable<MilvusQuerySegmentInfoResult> From(
        GetQuerySegmentInfoResponse response)
    {
        foreach (QuerySegmentInfo info in response.Infos)
        {
            yield return new MilvusQuerySegmentInfoResult()
            {
                CollectionId = info.CollectionID,
                IndexName = info.IndexName,
                IndexId = info.IndexID,
                MemSize = info.MemSize,
                NodeId = info.NodeID,
                NumRows = info.NumRows,
                PartitionId = info.PartitionID,
                SegmentId = info.SegmentID,
                State = (MilvusSegmentState)info.State
            };
        }
    }
}