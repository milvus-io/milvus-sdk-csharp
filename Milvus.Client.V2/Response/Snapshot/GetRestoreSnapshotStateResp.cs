namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// The state of a restore snapshot job, as returned by <c>GetRestoreSnapshotState</c>.
/// </summary>
public sealed class GetRestoreSnapshotStateResp
{
    private GetRestoreSnapshotStateResp(RestoreSnapshotJobInfo jobInfo) => JobInfo = jobInfo;

    internal static GetRestoreSnapshotStateResp FromGrpc(Grpc.GetRestoreSnapshotStateResponse response)
        => new(RestoreSnapshotJobInfo.FromGrpc(response.Info));

    /// <summary>
    /// The state of the restore job.
    /// </summary>
    public RestoreSnapshotJobInfo JobInfo { get; }
}
