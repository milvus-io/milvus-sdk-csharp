using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to list all compaction tasks still retained for a collection, whether
/// automatically or manually triggered. Terminal tasks are subject to server-side garbage collection
/// and are not an audit log.
/// </summary>
public sealed class ListCompactionTasksReq
{
    /// <summary>
    /// The name of the collection whose retained compaction tasks to list.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java SDK's request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    internal Grpc.GetCompactionPlansRequest ToGrpcGetCompactionPlansRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.GetCompactionPlansRequest { CollectionName = CollectionName };

        if (!string.IsNullOrEmpty(DatabaseName))
        {
            request.DbName = DatabaseName;
        }

        return request;
    }
}
