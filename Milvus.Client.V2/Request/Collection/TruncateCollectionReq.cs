using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to remove all entities from a collection.
/// </summary>
public sealed class TruncateCollectionReq
{
    /// <summary>
    /// The name of the collection to truncate.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.TruncateCollectionRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.TruncateCollectionRequest { CollectionName = CollectionName };
    }
}
