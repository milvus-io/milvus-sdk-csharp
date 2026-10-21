using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the progress of a refresh-external-collection job.
/// </summary>
public sealed class GetRefreshExternalCollectionProgressReq
{
    /// <summary>
    /// The job ID returned by <see cref="MilvusClientV2.RefreshExternalCollectionAsync" />.
    /// </summary>
    public long JobId { get; set; }

    internal void Validate()
    {
        Verify.GreaterThan(JobId, 0);
    }

    internal Grpc.GetRefreshExternalCollectionProgressRequest ToGrpcGetRefreshExternalCollectionProgressRequest()
        => new() { JobId = JobId };
}
