namespace Milvus.Client;

/// <summary>
/// The state of an compaction previously started via <see cref="MilvusCollection.CompactAsync" />, returned by
/// <see cref="MilvusClient.GetCompactionStateAsync" />.
/// </summary>
public enum CompactionState
{
    /// <summary>
    /// The provided compaction ID doesn't refer to a unknown compaction.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// The compaction is currently executing.
    /// </summary>
    Executing = 1,

    /// <summary>
    /// The compaction has completed.
    /// </summary>
    Completed = 2,
}
