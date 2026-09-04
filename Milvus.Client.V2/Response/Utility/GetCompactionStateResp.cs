using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The state of a compaction together with the per-plan execution counts.
/// </summary>
public sealed class GetCompactionStateResp
{
    internal GetCompactionStateResp(
        CompactionState state, long executingPlanNo, long timeoutPlanNo, long completedPlanNo, long failedPlanNo)
    {
        State = state;
        ExecutingPlanNo = executingPlanNo;
        TimeoutPlanNo = timeoutPlanNo;
        CompletedPlanNo = completedPlanNo;
        FailedPlanNo = failedPlanNo;
    }

    internal static GetCompactionStateResp FromGrpc(Grpc.GetCompactionStateResponse response)
        => new(
            (CompactionState)response.State,
            response.ExecutingPlanNo,
            response.TimeoutPlanNo,
            response.CompletedPlanNo,
            response.FailedPlanNo);

    /// <summary>
    /// The overall state of the compaction.
    /// </summary>
    public CompactionState State { get; }

    /// <summary>
    /// The number of merge plans currently executing.
    /// </summary>
    public long ExecutingPlanNo { get; }

    /// <summary>
    /// The number of merge plans that timed out.
    /// </summary>
    public long TimeoutPlanNo { get; }

    /// <summary>
    /// The number of merge plans that completed.
    /// </summary>
    public long CompletedPlanNo { get; }

    /// <summary>
    /// The number of merge plans that failed.
    /// </summary>
    public long FailedPlanNo { get; }
}
