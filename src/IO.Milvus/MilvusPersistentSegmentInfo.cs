using Google.Protobuf.Collections;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// Milvus segment state.
/// </summary>
public enum MilvusSegmentState
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Not exist.
    /// </summary>
    NotExist = 1,


    Growing = 2,
    Sealed = 3,
    Flushed = 4,
    Flushing = 5,
    Dropped = 6,
    Importing = 7,
}

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
    public MilvusSegmentState State { get; }

    internal static IEnumerable<MilvusPersistentSegmentInfo> From(
        IEnumerable<PersistentSegmentInfo> infos)
    {
        foreach (var info in infos)
        {
            yield return new MilvusPersistentSegmentInfo(
                info.CollectionID,
                info.PartitionID,
                info.SegmentID,
                info.NumRows,
                info.State
                );
        }
    }

    #region Private ===========================================================
    private MilvusPersistentSegmentInfo(
    long collectionID,
    long partitionID,
    long segmentID,
    long numRows,
    SegmentState state)
    {
        this.CollectionId = collectionID;
        this.PartitionId = partitionID;
        this.SegmentId = segmentID;
        this.NumRows = numRows;
        this.State = (MilvusSegmentState)state;
    }
    #endregion
}
