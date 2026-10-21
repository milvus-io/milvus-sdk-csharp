namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <c>GetRefreshExternalCollectionProgress</c> operation.
/// </summary>
public sealed class GetRefreshExternalCollectionProgressResp
{
    private GetRefreshExternalCollectionProgressResp(RefreshExternalCollectionJobInfo jobInfo) => JobInfo = jobInfo;

    internal static GetRefreshExternalCollectionProgressResp FromGrpc(Grpc.GetRefreshExternalCollectionProgressResponse response)
        => new(RefreshExternalCollectionJobInfo.FromGrpc(response.JobInfo));

    /// <summary>
    /// The refresh job information.
    /// </summary>
    public RefreshExternalCollectionJobInfo JobInfo { get; }
}
