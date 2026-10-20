using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Metadata and progress of a refresh-external-collection job.
/// </summary>
public sealed class RefreshExternalCollectionJobInfo
{
    private RefreshExternalCollectionJobInfo(
        long jobId, string collectionName, RefreshExternalCollectionState state, long progress, string reason,
        string externalSource, long startTime, long endTime, string externalSpec)
    {
        JobId = jobId;
        CollectionName = collectionName;
        State = state;
        Progress = progress;
        Reason = reason;
        ExternalSource = externalSource;
        StartTime = startTime;
        EndTime = endTime;
        ExternalSpec = externalSpec;
    }

    internal static RefreshExternalCollectionJobInfo FromGrpc(Grpc.RefreshExternalCollectionJobInfo info)
        => new(
            info.JobId, info.CollectionName, (RefreshExternalCollectionState)info.State, info.Progress,
            info.Reason, info.ExternalSource, info.StartTime, info.EndTime, info.ExternalSpec);

    /// <summary>
    /// The job identifier.
    /// </summary>
    public long JobId { get; }

    /// <summary>
    /// The name of the collection being refreshed.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The current state of the refresh job.
    /// </summary>
    public RefreshExternalCollectionState State { get; }

    /// <summary>
    /// The refresh progress in percent (0 to 100).
    /// </summary>
    public long Progress { get; }

    /// <summary>
    /// The failure reason of the job, empty when the job has not failed.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// The external source used for this job.
    /// </summary>
    public string ExternalSource { get; }

    /// <summary>
    /// The start timestamp of the job (Unix epoch in milliseconds), 0 if not started.
    /// </summary>
    public long StartTime { get; }

    /// <summary>
    /// The end timestamp of the job (Unix epoch in milliseconds), 0 if not completed.
    /// </summary>
    public long EndTime { get; }

    /// <summary>
    /// The external spec used for this job.
    /// </summary>
    public string ExternalSpec { get; }
}
