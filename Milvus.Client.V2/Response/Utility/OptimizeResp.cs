namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of an optimize operation, mirroring the Java SDK's <c>OptimizeResp</c>.
/// </summary>
public sealed class OptimizeResp
{
    internal OptimizeResp(
        string status, string collectionName, long? compactionId, string? targetSize,
        IReadOnlyList<string> progress)
    {
        Status = status;
        CollectionName = collectionName;
        CompactionId = compactionId;
        TargetSize = targetSize;
        Progress = progress;
    }

    /// <summary>
    /// The overall result status; <c>"success"</c> when the optimization completed without error.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// The name of the optimized collection (SDK-derived from the request).
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The ID of the compaction triggered by the optimization, or <c>null</c> if none was triggered.
    /// </summary>
    public long? CompactionId { get; }

    /// <summary>
    /// The target segment size used for the compaction, in MB.
    /// </summary>
    public string? TargetSize { get; }

    /// <summary>
    /// The stages the optimization passed through, e.g. <c>waiting for indexes</c>,
    /// <c>compacting</c>, <c>waiting for compaction</c>, <c>refreshing load</c>. Mirrors the Java SDK's
    /// <c>OptimizeResp.progress</c>.
    /// </summary>
    public IReadOnlyList<string> Progress { get; }
}
