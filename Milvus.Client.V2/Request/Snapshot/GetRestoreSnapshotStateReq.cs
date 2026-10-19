using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to query the status and progress of a restore snapshot job.
/// </summary>
public sealed class GetRestoreSnapshotStateReq
{
    /// <summary>
    /// The ID of the restore job to query, as returned by <see cref="MilvusClientV2.RestoreSnapshotAsync" />.
    /// </summary>
    public long JobId { get; set; }

    internal Grpc.GetRestoreSnapshotStateRequest ToGrpcGetRestoreSnapshotStateRequest()
    {
        Verify.GreaterThan(JobId, 0);
        return new Grpc.GetRestoreSnapshotStateRequest { JobId = JobId };
    }
}
