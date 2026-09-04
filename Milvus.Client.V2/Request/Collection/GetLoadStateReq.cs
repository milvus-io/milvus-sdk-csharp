using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to get the load state of a collection.
/// </summary>
public sealed class GetLoadStateReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.GetLoadStateRequest ToGrpcGetLoadStateRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.GetLoadStateRequest { CollectionName = CollectionName };
    }
}
