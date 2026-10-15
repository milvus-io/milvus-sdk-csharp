using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to check whether a collection exists.
/// </summary>
public sealed class HasCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to check.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.HasCollectionRequest ToGrpcHasCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.HasCollectionRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
