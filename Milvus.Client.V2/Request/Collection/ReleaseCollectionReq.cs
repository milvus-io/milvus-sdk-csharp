using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to release a loaded collection from memory.
/// </summary>
public sealed class ReleaseCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to release.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.ReleaseCollectionRequest ToGrpcReleaseCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        var request = new Grpc.ReleaseCollectionRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
