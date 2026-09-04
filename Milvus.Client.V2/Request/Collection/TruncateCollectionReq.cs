using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to remove all entities from a collection.
/// </summary>
public sealed class TruncateCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to truncate.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.TruncateCollectionRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.TruncateCollectionRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
