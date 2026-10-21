namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <c>ListRefreshExternalCollectionJobs</c> operation.
/// </summary>
public sealed class ListRefreshExternalCollectionJobsResp
{
    private ListRefreshExternalCollectionJobsResp(IReadOnlyList<RefreshExternalCollectionJobInfo> jobs) => Jobs = jobs;

    internal static ListRefreshExternalCollectionJobsResp FromGrpc(Grpc.ListRefreshExternalCollectionJobsResponse response)
        => new(response.Jobs.Select(RefreshExternalCollectionJobInfo.FromGrpc).ToList());

    /// <summary>
    /// The refresh jobs.
    /// </summary>
    public IReadOnlyList<RefreshExternalCollectionJobInfo> Jobs { get; }
}
