using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Information about a replica of a loaded collection, as returned by the <c>describeReplicas</c> API.
/// </summary>
public sealed class ReplicaInfo
{
    internal ReplicaInfo(
        long replicaId,
        long collectionId,
        IReadOnlyList<long> partitionIds,
        IReadOnlyList<ShardReplica> shardReplicas,
        IReadOnlyList<long> nodeIds,
        string resourceGroupName,
        IReadOnlyDictionary<string, int> numOutboundNode)
    {
        ReplicaId = replicaId;
        CollectionId = collectionId;
        PartitionIds = partitionIds;
        ShardReplicas = shardReplicas;
        NodeIds = nodeIds;
        ResourceGroupName = resourceGroupName;
        NumOutboundNode = numOutboundNode;
    }

    /// <summary>
    /// The ID of the replica.
    /// </summary>
    public long ReplicaId { get; }

    /// <summary>
    /// The ID of the collection to which the replica belongs.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// The IDs of the partitions loaded on the replica. An empty list indicates the whole collection is loaded.
    /// </summary>
    public IReadOnlyList<long> PartitionIds { get; }

    /// <summary>
    /// The shard replicas that serve the shard channels of the collection.
    /// </summary>
    public IReadOnlyList<ShardReplica> ShardReplicas { get; }

    /// <summary>
    /// The IDs of the query nodes that host the replica, including the leaders.
    /// </summary>
    public IReadOnlyList<long> NodeIds { get; }

    /// <summary>
    /// The name of the resource group to which the replica belongs.
    /// </summary>
    public string ResourceGroupName { get; }

    /// <summary>
    /// The number of outbound query nodes contributed by each resource group.
    /// </summary>
    public IReadOnlyDictionary<string, int> NumOutboundNode { get; }

    internal static ReplicaInfo FromGrpc(Grpc.ReplicaInfo replica)
        => new(
            replica.ReplicaID,
            replica.CollectionID,
            replica.PartitionIds.ToList(),
            replica.ShardReplicas.Select(ShardReplica.FromGrpc).ToList(),
            replica.NodeIds.ToList(),
            replica.ResourceGroupName,
            replica.NumOutboundNode.ToDictionary(kv => kv.Key, kv => kv.Value));
}
