namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The outcome of a <see cref="MilvusClientV2.ListRunningRequestsAsync" /> or
/// <see cref="MilvusClientV2.CancelRequestsAsync" /> call on one proxy.
/// </summary>
/// <remarks>
/// Both calls are sent to every proxy. A proxy that did not answer contributed nothing to the result; its requests
/// are missing from it.
/// </remarks>
public sealed class RunningRequestNodeResult
{
    private RunningRequestNodeResult(long nodeId, int errorCode, string reason, bool unimplemented)
    {
        NodeId = nodeId;
        ErrorCode = errorCode;
        Reason = reason;
        Unimplemented = unimplemented;
    }

    internal static RunningRequestNodeResult FromGrpc(Grpc.RunningRequestNodeResult nodeResult)
    {
        int code = nodeResult.Status.Code;

        // Only servers that implement the new RPCs produce a RunningRequestNodeResult, and they always populate
        // code, so the legacy error_code fallback is defensive: mirror the Java SDK's convertRunningRequestNodeResults
        // so a node answer that reports only the deprecated field is not silently taken for a success.
#pragma warning disable CS0612 // ErrorCode is [deprecated] but required for the legacy fallback.
        if (code == 0 && nodeResult.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            code = (int)nodeResult.Status.ErrorCode;
        }
#pragma warning restore CS0612

        return new(nodeResult.NodeId, code, nodeResult.Status.Reason, nodeResult.Unimplemented);
    }

    /// <summary>
    /// The node id of the proxy.
    /// </summary>
    public long NodeId { get; }

    /// <summary>
    /// The error code of the proxy's answer; 0 means the proxy answered.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// The error message when the proxy did not answer.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Whether the proxy is too old to serve the call, as during a rolling upgrade. Such a proxy holds no listable
    /// requests.
    /// </summary>
    public bool Unimplemented { get; }

    /// <summary>
    /// Whether the proxy answered the call.
    /// </summary>
    public bool IsSuccess => ErrorCode == 0;
}
