using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop a collection.
/// </summary>
public sealed class DropCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to drop.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.DropCollectionRequest ToGrpcDropCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.DropCollectionRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
