namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <c>RefreshExternalCollection</c> operation.
/// </summary>
public sealed class RefreshExternalCollectionResp
{
    private RefreshExternalCollectionResp(long jobId) => JobId = jobId;

    internal static RefreshExternalCollectionResp FromGrpc(Grpc.RefreshExternalCollectionResponse response)
        => new(response.JobId);

    /// <summary>
    /// The unique job identifier for tracking the refresh progress via
    /// <see cref="MilvusClientV2.GetRefreshExternalCollectionProgressAsync" />.
    /// </summary>
    public long JobId { get; }
}
