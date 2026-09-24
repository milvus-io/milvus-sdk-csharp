using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to describe a collection.
/// </summary>
public sealed class DescribeCollectionReq
{
    /// <summary>
    /// The name of the collection to describe.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    internal Grpc.DescribeCollectionRequest ToGrpcDescribeCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.DescribeCollectionRequest
        {
            DbName = DatabaseName ?? "",
            CollectionName = CollectionName
        };
    }
}
