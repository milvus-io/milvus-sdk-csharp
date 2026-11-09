namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <see cref="MilvusClientV2.ListRunningRequestsAsync" /> operation.
/// </summary>
public sealed class ListRunningRequestsResp
{
    private ListRunningRequestsResp(
        IReadOnlyList<RunningRequestInfo> requests, IReadOnlyList<RunningRequestNodeResult> nodeResults)
    {
        Requests = requests;
        NodeResults = nodeResults;
    }

    internal static ListRunningRequestsResp FromGrpc(Grpc.ListRunningRequestsResponse response)
        => new(
            response.Requests.Select(RunningRequestInfo.FromGrpc).ToList(),
            response.NodeResults.Select(RunningRequestNodeResult.FromGrpc).ToList());

    /// <summary>
    /// The running requests reported by the proxies that answered. A proxy that did not answer contributes nothing
    /// here; it is reported in <see cref="NodeResults" /> instead.
    /// </summary>
    public IReadOnlyList<RunningRequestInfo> Requests { get; }

    /// <summary>
    /// The outcome on each proxy: one entry per proxy the call was sent to. A proxy that did not answer has a
    /// non-zero <see cref="RunningRequestNodeResult.ErrorCode" /> and its requests are missing from
    /// <see cref="Requests" />.
    /// </summary>
    public IReadOnlyList<RunningRequestNodeResult> NodeResults { get; }
}
