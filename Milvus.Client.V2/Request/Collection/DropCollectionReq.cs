using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop a collection.
/// </summary>
public sealed class DropCollectionReq
{
    /// <summary>
    /// The name of the collection to drop.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.DropCollectionRequest ToGrpcDropCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.DropCollectionRequest { CollectionName = CollectionName };
    }
}
