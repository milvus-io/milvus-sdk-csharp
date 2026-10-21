namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to list refresh-external-collection jobs.
/// </summary>
public sealed class ListRefreshExternalCollectionJobsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// An optional collection name to filter by. When empty, jobs for all collections in the database are listed.
    /// </summary>
    public string? CollectionName { get; set; }

    internal Grpc.ListRefreshExternalCollectionJobsRequest ToGrpcListRefreshExternalCollectionJobsRequest(string databaseName)
        => new()
        {
            DbName = databaseName,
            CollectionName = CollectionName ?? ""
        };
}
