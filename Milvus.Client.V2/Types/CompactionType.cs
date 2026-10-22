namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the type of a compaction task.
/// </summary>
public enum CompactionType
{
    /// <summary>
    /// The compaction type is undefined.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// The compaction merges segments.
    /// </summary>
    Merge = 2,

    /// <summary>
    /// The compaction mixes multiple segment operations.
    /// </summary>
    Mix = 3,

    /// <summary>
    /// The compaction acts on a single segment.
    /// </summary>
    Single = 4,

    /// <summary>
    /// The compaction is a minor compaction.
    /// </summary>
    Minor = 5,

    /// <summary>
    /// The compaction is a major compaction.
    /// </summary>
    Major = 6,

    /// <summary>
    /// The compaction removes level-0 delete records.
    /// </summary>
    Level0Delete = 7,

    /// <summary>
    /// The compaction is a clustering compaction.
    /// </summary>
    Clustering = 8,

    /// <summary>
    /// The compaction sorts the segment.
    /// </summary>
    Sort = 9,

    /// <summary>
    /// The compaction sorts the segment by partition key.
    /// </summary>
    PartitionKeySort = 10,

    /// <summary>
    /// The compaction is a clustering and partition-key sort compaction.
    /// </summary>
    ClusteringPartitionKeySort = 11,

    /// <summary>
    /// The compaction bumps the schema version.
    /// </summary>
    BumpSchemaVersion = 12
}
