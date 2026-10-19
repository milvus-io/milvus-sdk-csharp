namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// The result of a <c>ListRestoreSnapshotJobs</c> operation.
/// </summary>
public sealed class ListRestoreSnapshotJobsResp
{
    private ListRestoreSnapshotJobsResp(IReadOnlyList<RestoreSnapshotJobInfo> jobs) => Jobs = jobs;

    internal static ListRestoreSnapshotJobsResp FromGrpc(Grpc.ListRestoreSnapshotJobsResponse response)
        => new(response.Jobs.Select(RestoreSnapshotJobInfo.FromGrpc).ToList());

    /// <summary>
    /// The listed restore snapshot jobs.
    /// </summary>
    public IReadOnlyList<RestoreSnapshotJobInfo> Jobs { get; }
}
