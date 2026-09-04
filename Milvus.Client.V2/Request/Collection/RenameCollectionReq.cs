using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to rename a collection.
/// </summary>
public sealed class RenameCollectionReq
{
    /// <summary>
    /// The current name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The new name of the collection.
    /// </summary>
    public string NewCollectionName { get; set; } = "";

    internal Grpc.RenameCollectionRequest ToGrpcRenameCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(NewCollectionName);
        return new Grpc.RenameCollectionRequest
        {
            OldName = CollectionName,
            NewName = NewCollectionName
        };
    }
}
