using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to load a collection into memory.
/// </summary>
public sealed class LoadCollectionReq
{
    /// <summary>
    /// The name of the collection to load.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The replica number to load. Defaults to 1.
    /// </summary>
    public int ReplicaNumber { get; set; } = 1;

    internal Grpc.LoadCollectionRequest ToGrpcLoadCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.LoadCollectionRequest { CollectionName = CollectionName, ReplicaNumber = ReplicaNumber };
    }
}
