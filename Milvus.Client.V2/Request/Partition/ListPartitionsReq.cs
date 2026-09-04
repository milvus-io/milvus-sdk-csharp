using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;

/// <summary>
/// Represents a request to list the partitions of a collection.
/// </summary>
public sealed class ListPartitionsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to list the partitions of.
    /// </summary>
    public string CollectionName { get; set; } = "";
    internal Grpc.ShowPartitionsRequest ToGrpcShowPartitionsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        var request = new Grpc.ShowPartitionsRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
