using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// The state of a single restore snapshot job, as returned by <c>GetRestoreSnapshotState</c> and
/// <c>ListRestoreSnapshotJobs</c>.
/// </summary>
public sealed class RestoreSnapshotJobInfo
{
    private RestoreSnapshotJobInfo(
        long jobId, string snapshotName, string dbName, string collectionName, RestoreSnapshotState state,
        int progress, string reason, ulong startTime, ulong timeCost)
    {
        JobId = jobId;
        SnapshotName = snapshotName;
        DbName = dbName;
        CollectionName = collectionName;
        State = state;
        Progress = progress;
        Reason = reason;
        StartTime = startTime;
        TimeCost = timeCost;
    }

    internal static RestoreSnapshotJobInfo FromGrpc(Grpc.RestoreSnapshotInfo info)
        => new(
            info.JobId,
            info.SnapshotName,
            info.DbName,
            info.CollectionName,
            (RestoreSnapshotState)info.State,
            info.Progress,
            info.Reason,
            info.StartTime,
            info.TimeCost);

    /// <summary>
    /// The ID of the restore job.
    /// </summary>
    public long JobId { get; }

    /// <summary>
    /// The name of the snapshot being restored.
    /// </summary>
    public string SnapshotName { get; }

    /// <summary>
    /// The database being restored into.
    /// </summary>
    public string DbName { get; }

    /// <summary>
    /// The collection being restored into.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The current state of the restore job.
    /// </summary>
    public RestoreSnapshotState State { get; }

    /// <summary>
    /// The progress percentage of the restore job (0-100).
    /// </summary>
    public int Progress { get; }

    /// <summary>
    /// The reason for failure, when the job failed.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// The start timestamp of the restore job, in milliseconds.
    /// </summary>
    public ulong StartTime { get; }

    /// <summary>
    /// The time cost of the restore job, in milliseconds.
    /// </summary>
    public ulong TimeCost { get; }
}
