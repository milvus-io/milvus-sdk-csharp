namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to list the Search, HybridSearch and Query requests running on every proxy of the
/// cluster.
/// </summary>
public sealed class ListRunningRequestsReq
{
    /// <summary>
    /// The database name to match exactly. When empty (default), requests of every database are listed.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>
    /// The collection name to match exactly. When empty (default), requests of every collection are listed.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The user whose requests to list. When empty (default), every user's requests are listed. A caller without
    /// the <c>ListRunningRequests</c> privilege may only see their own requests: empty then means the caller, and
    /// any other name is denied.
    /// </summary>
    public string User { get; set; } = "";

    /// <summary>
    /// The minimum time, in milliseconds, a request has been running. A non-positive value (the default zero) means
    /// no constraint.
    /// </summary>
    public long MinElapsedMs { get; set; }

    internal Grpc.ListRunningRequestsRequest ToGrpcListRunningRequestsRequest()
    {
        var request = new Grpc.ListRunningRequestsRequest();
        if (!string.IsNullOrEmpty(DatabaseName))
        {
            request.DbName = DatabaseName;
        }

        if (!string.IsNullOrEmpty(CollectionName))
        {
            request.CollectionName = CollectionName;
        }

        if (!string.IsNullOrEmpty(User))
        {
            request.User = User;
        }

        // Only a positive elapsed threshold constrains the result; zero (or a negative value) means no
        // constraint, mirroring the Java SDK which only sets min_elapsed_ms when it is > 0.
        if (MinElapsedMs > 0)
        {
            request.MinElapsedMs = MinElapsedMs;
        }

        return request;
    }
}
