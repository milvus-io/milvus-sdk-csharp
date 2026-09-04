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

    internal Grpc.DescribeCollectionRequest ToGrpcDescribeCollectionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.DescribeCollectionRequest { CollectionName = CollectionName };
    }
}
