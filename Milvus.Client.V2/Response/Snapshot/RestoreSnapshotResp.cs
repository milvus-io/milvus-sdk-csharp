namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// The result of a <c>RestoreSnapshot</c> operation, carrying the ID of the asynchronous restore job.
/// </summary>
public sealed class RestoreSnapshotResp
{
    private RestoreSnapshotResp(long jobId) => JobId = jobId;

    internal static RestoreSnapshotResp FromGrpc(Grpc.RestoreSnapshotResponse response)
        => new(response.JobId);

    /// <summary>
    /// The ID of the restore job, used to track progress via
    /// <see cref="MilvusClientV2.GetRestoreSnapshotStateAsync" />.
    /// </summary>
    public long JobId { get; }
}
