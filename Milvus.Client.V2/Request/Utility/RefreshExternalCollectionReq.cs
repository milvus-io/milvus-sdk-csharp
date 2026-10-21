using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to refresh an external collection from its external data source.
/// </summary>
public sealed class RefreshExternalCollectionReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the external collection to refresh.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional new external data source path (e.g. an S3 path). When empty, the collection's existing
    /// external source is kept.
    /// </summary>
    public string? ExternalSource { get; set; }

    /// <summary>
    /// An optional new external spec configuration (as a JSON string). When empty, the collection's existing
    /// external spec is kept.
    /// </summary>
    public string? ExternalSpec { get; set; }

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
    }

    internal Grpc.RefreshExternalCollectionRequest ToGrpcRefreshExternalCollectionRequest(string databaseName)
        => new()
        {
            DbName = databaseName,
            CollectionName = CollectionName,
            ExternalSource = ExternalSource ?? "",
            ExternalSpec = ExternalSpec ?? ""
        };
}
