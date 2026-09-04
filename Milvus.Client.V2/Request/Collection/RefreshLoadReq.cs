using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to refresh the loaded data of a collection.
/// </summary>
public sealed class RefreshLoadReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

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
    public long? TimeoutMilliseconds { get; set; } = 60000;

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
    }

    internal Grpc.LoadCollectionRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.LoadCollectionRequest
        {
            CollectionName = CollectionName,
            Refresh = true
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
