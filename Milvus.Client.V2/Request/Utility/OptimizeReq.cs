using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to optimize a collection (rebuilding indexes and compacting segments).
/// </summary>
public sealed class OptimizeReq
{
    /// <summary>
    /// The name of the collection to optimize.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional target segment size in MB for the compaction.
    /// </summary>
    public long? TargetSizeInMB { get; set; }

    /// <summary>
    /// Whether to block until the optimization completes. Defaults to <c>true</c>.
    /// </summary>
    public bool WaitForCompletion { get; set; } = true;

    /// <summary>
    /// The timeout in milliseconds for waiting for the optimization to complete. Defaults to no timeout.
    /// </summary>
    public long? TimeoutMilliseconds { get; set; }

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
    }
}
