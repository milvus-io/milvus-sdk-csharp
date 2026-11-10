namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <see cref="MilvusClientV2.CancelRequestsAsync" /> operation.
/// </summary>
/// <remarks>
/// Cancellation is asynchronous: a request in <see cref="Canceled" /> has been told to stop and does so at its next
/// cancellation check, usually within milliseconds, then leaves the list. Until then
/// <see cref="MilvusClientV2.ListRunningRequestsAsync" /> shows it in state <c>Canceling</c>. Canceling the same id
/// again before it has left is harmless: it is returned in <see cref="Canceled" /> once more, in state
/// <c>Canceling</c>, and is not audited or counted a second time. Every requested id is in exactly one of
/// <see cref="Canceled" />, <see cref="NotFound" /> and <see cref="Undetermined" />.
/// </remarks>
public sealed class CancelRequestsResp
{
    private CancelRequestsResp(
        IReadOnlyList<RunningRequestInfo> canceled, IReadOnlyList<string> notFound,
        IReadOnlyList<string> undetermined, IReadOnlyList<RunningRequestNodeResult> nodeResults)
    {
        Canceled = canceled;
        NotFound = notFound;
        Undetermined = undetermined;
        NodeResults = nodeResults;
    }

    internal static CancelRequestsResp FromGrpc(Grpc.CancelRequestsResponse response)
        => new(
            response.Canceled.Select(RunningRequestInfo.FromGrpc).ToList(),
            response.NotFound.ToList(),
            response.Undetermined.ToList(),
            response.NodeResults.Select(RunningRequestNodeResult.FromGrpc).ToList());

    /// <summary>
    /// One entry per canceled request, describing it as it was at the moment of cancellation: <c>ElapsedMs</c> is how
    /// long it had been running when it was stopped, and <c>State</c> is the state it was in before it was canceled
    /// (queued or running), or <c>Canceling</c> when this call repeated an earlier cancel.
    /// </summary>
    public IReadOnlyList<RunningRequestInfo> Canceled { get; }

    /// <summary>
    /// The ids that no proxy holds, known because every proxy answered: the request already finished, never existed,
    /// or (when <c>User</c> is set) was issued by another user. A proxy too old to serve this call counts as having
    /// answered, since it registers no requests. Empty whenever some proxy did not answer; those ids are reported in
    /// <see cref="Undetermined" /> instead.
    /// </summary>
    public IReadOnlyList<string> NotFound { get; }

    /// <summary>
    /// The ids not canceled while some proxy did not answer (see <see cref="NodeResults" />): the cancel is sent to
    /// every proxy, so each of these may be running on a proxy that was not reached, or may have finished. Retry the
    /// cancel, or list again, once that proxy is back.
    /// </summary>
    public IReadOnlyList<string> Undetermined { get; }

    /// <summary>
    /// The outcome on each proxy: one entry per proxy the call was sent to. A proxy that did not answer has a
    /// non-zero <see cref="RunningRequestNodeResult.ErrorCode" />.
    /// </summary>
    public IReadOnlyList<RunningRequestNodeResult> NodeResults { get; }
}
