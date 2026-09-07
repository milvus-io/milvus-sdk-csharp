using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to drop the properties of a collection.
/// </summary>
public sealed class DropCollectionPropertiesReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The names of the properties to remove from the collection.
    /// </summary>
    public IReadOnlyList<string> DeleteKeys { get; set; } = Array.Empty<string>();

    internal Grpc.AlterCollectionRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(DeleteKeys);

        var request = new Grpc.AlterCollectionRequest { CollectionName = CollectionName };
        request.DeleteKeys.AddRange(DeleteKeys);
        return request;
    }
}
