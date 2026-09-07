using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to refresh the loaded data of a collection.
/// </summary>
public sealed class RefreshLoadReq
{
    /// <summary>
    /// The name of the collection to refresh.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// Whether to wait until the collection is fully loaded again. Defaults to <c>true</c>.
    /// </summary>
    public bool Sync { get; set; } = true;

    /// <summary>
    /// The timeout for waiting for the collection to be fully loaded, in milliseconds. Defaults to 60000.
    /// </summary>
    public long? TimeoutMilliseconds { get; set; }

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
    }

    internal Grpc.LoadCollectionRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.LoadCollectionRequest
        {
            CollectionName = CollectionName,
            Refresh = true
        };
    }
}
