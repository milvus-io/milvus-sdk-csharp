namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a compaction operation.
/// </summary>
public sealed class CompactResp
{
    internal CompactResp(long compactionId, int compactionPlanCount)
    {
        CompactionId = compactionId;
        CompactionPlanCount = compactionPlanCount;
    }

    internal static CompactResp FromGrpc(Grpc.ManualCompactionResponse response)
        => new(response.CompactionID, response.CompactionPlanCount);

    /// <summary>
    /// The id of the compaction, used to poll its state and plans.
    /// </summary>
    public long CompactionId { get; }

    /// <summary>
    /// The number of compaction plans created for this compaction, matching the C++ SDK's
    /// <c>CompactResponse.CompactionPlanCount()</c>.
    /// </summary>
    public int CompactionPlanCount { get; }
}
