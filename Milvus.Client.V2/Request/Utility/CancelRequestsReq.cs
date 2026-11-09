using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to cancel running requests by id, as reported by
/// <see cref="MilvusClientV2.ListRunningRequestsAsync" />.
/// </summary>
public sealed class CancelRequestsReq
{
    /// <summary>
    /// The ids of the requests to cancel, exactly as returned by <see cref="MilvusClientV2.ListRunningRequestsAsync" />.
    /// Must not be empty; request ids are opaque strings such as <c>"25-to03zu-rs"</c>.
    /// </summary>
    public IReadOnlyList<string> RequestIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// A free-text reason, recorded in the server's audit log and returned to the client whose request is canceled.
    /// </summary>
    public string Reason { get; set; } = "";

    /// <summary>
    /// When set, only requests issued by this user are canceled; the ids of other users' requests are reported as
    /// not found. A caller without the <c>CancelRequests</c> privilege may only cancel their own requests: empty
    /// then means the caller, and any other name is denied.
    /// </summary>
    public string User { get; set; } = "";

    internal void Validate() => Verify.NotNullOrEmpty(RequestIds);

    internal Grpc.CancelRequestsRequest ToGrpcCancelRequestsRequest()
    {
        var request = new Grpc.CancelRequestsRequest();
        request.RequestIds.AddRange(RequestIds);
        if (!string.IsNullOrEmpty(Reason))
        {
            request.Reason = Reason;
        }

        if (!string.IsNullOrEmpty(User))
        {
            request.User = User;
        }

        return request;
    }
}
