using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to describe the replicas of a loaded collection.
/// </summary>
public sealed class DescribeReplicasReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// Whether to also return the query node IDs of each shard replica.
    /// </summary>
    public bool WithShardNodes { get; set; }

    internal Grpc.GetReplicasRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        return new Grpc.GetReplicasRequest
        {
            CollectionName = CollectionName,
            WithShardNodes = WithShardNodes
        };
    }
}
