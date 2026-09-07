namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of an optimize operation.
/// </summary>
public sealed class OptimizeResp
{
    internal OptimizeResp(long? compactionId, string? targetSize)
    {
        CompactionId = compactionId;
        TargetSize = targetSize;
    }

    /// <summary>
    /// The ID of the compaction triggered by the optimization, or <c>null</c> if none was triggered.
    /// </summary>
    public long? CompactionId { get; }

    /// <summary>
    /// The target segment size used for the compaction, in MB.
    /// </summary>
    public string? TargetSize { get; }
}
