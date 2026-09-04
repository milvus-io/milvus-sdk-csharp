using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to release a loaded collection from memory.
/// </summary>
public sealed class ReleaseCollectionReq
{
    /// <summary>
    /// The name of the collection to release.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.ReleaseCollectionRequest ToGrpcReleaseCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.ReleaseCollectionRequest { CollectionName = CollectionName };
    }
}
