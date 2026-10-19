using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to list restore snapshot jobs.
/// </summary>
public sealed class ListRestoreSnapshotJobsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the target collection to filter jobs for. When empty, all restore jobs are listed.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.ListRestoreSnapshotJobsRequest ToGrpcListRestoreSnapshotJobsRequest()
    {
        var request = new Grpc.ListRestoreSnapshotJobsRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
