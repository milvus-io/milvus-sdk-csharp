using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus query segment result.
/// </summary>
public class MilvusQuerySegmentInfoResult
{
    /// <summary>
    /// Collection id.
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// Index name.
    /// </summary>
    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    /// <summary>
    /// Index id.
    /// </summary>
    [JsonPropertyName("indexID")]
    public long IndexId { get; set; }

    /// <summary>
    /// Memory size.
    /// </summary>
    [JsonPropertyName("mem_size")]
    public long MemSize { get; set; }

    /// <summary>
    /// Node id.
    /// </summary>
    [JsonPropertyName("nodeID")]
    public long NodeId { get; set; }

    /// <summary>
    /// Number of rows.
    /// </summary>
    [JsonPropertyName("num_rows")]
    public long NumRows { get; set; }

    /// <summary>
    /// Partition id.
    /// </summary>
    [JsonPropertyName("partitionID")]
    public long PartitionId { get; set; }

    /// <summary>
    /// Segment id.
    /// </summary>
    [JsonPropertyName("segmentID")]
    public long SegmentId { get; set; }

    /// <summary>
    /// State.
    /// </summary>
    [JsonPropertyName("state")]
    public MilvusSegmentState State { get; set; }

    internal static IEnumerable<MilvusQuerySegmentInfoResult> From(
        GetQuerySegmentInfoResponse response)
    {
        foreach (var info in response.Infos)
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