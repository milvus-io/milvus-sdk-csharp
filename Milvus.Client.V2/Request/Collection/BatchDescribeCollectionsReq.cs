using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to describe multiple collections at once, by name and/or collection ID.
/// </summary>
public sealed class BatchDescribeCollectionsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The names of the collections to describe.
    /// </summary>
    public IReadOnlyList<string> CollectionNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The IDs of the collections to describe.
    /// </summary>
    public IReadOnlyList<long> CollectionIds { get; set; } = Array.Empty<long>();

    internal Grpc.BatchDescribeCollectionRequest ToGrpcBatchDescribeCollectionRequest()
    {
        Verify.NotNull(CollectionNames);
        Verify.NotNull(CollectionIds);

        if (CollectionNames.Count == 0 && CollectionIds.Count == 0)
        {
            throw new ArgumentException("At least one collection name or collection ID must be provided.");
        }

        var request = new Grpc.BatchDescribeCollectionRequest();
        request.CollectionName.AddRange(CollectionNames);
        request.CollectionID.AddRange(CollectionIds);
        request.DbName = DatabaseName ?? "";
        return request;
    }
}