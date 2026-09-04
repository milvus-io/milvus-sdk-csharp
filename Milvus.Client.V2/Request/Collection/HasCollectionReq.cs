using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to check whether a collection exists.
/// </summary>
public sealed class HasCollectionReq
{
    /// <summary>
    /// The name of the collection to check.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.HasCollectionRequest ToGrpcHasCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.HasCollectionRequest { CollectionName = CollectionName };
    }
}
