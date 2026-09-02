namespace Milvus.Client;

/// <summary>
/// Describes one in-memory replica of a loaded collection.
/// </summary>
public sealed class MilvusReplicaInfo
{
    internal MilvusReplicaInfo(
        long replicaId,
        long collectionId,
        IReadOnlyList<long> partitionIds,
        IReadOnlyList<MilvusShardReplica> shardReplicas,
        IReadOnlyList<long> nodeIds,
        string resourceGroupName,
        IReadOnlyDictionary<string, int> outboundNodeCounts)
    {
        ReplicaId = replicaId;
        CollectionId = collectionId;
        PartitionIds = partitionIds;
        ShardReplicas = shardReplicas;
        NodeIds = nodeIds;
        ResourceGroupName = resourceGroupName;
        OutboundNodeCounts = outboundNodeCounts;
    }

    /// <summary>
    /// The replica id.
    /// </summary>
    public long ReplicaId { get; }

    /// <summary>
    /// The id of the collection this replica belongs to.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// The partitions covered by this replica. Empty means the whole collection is loaded.
    /// </summary>
    public IReadOnlyList<long> PartitionIds { get; }

    /// <summary>
    /// The per-shard replicas making up this replica.
    /// </summary>
    public IReadOnlyList<MilvusShardReplica> ShardReplicas { get; }

    /// <summary>
    /// The query nodes serving this replica, including shard leaders.
    /// </summary>
    public IReadOnlyList<long> NodeIds { get; }

    /// <summary>
    /// The resource group this replica is loaded into.
    /// </summary>
    public string ResourceGroupName { get; }

    /// <summary>
    /// Resource group name to the number of its nodes this replica is borrowing.
    /// </summary>
    public IReadOnlyDictionary<string, int> OutboundNodeCounts { get; }

    internal static MilvusReplicaInfo FromGrpc(Grpc.ReplicaInfo replica)
        => new(
            replica.ReplicaID,
            replica.CollectionID,
            replica.PartitionIds,
            replica.ShardReplicas.Select(MilvusShardReplica.FromGrpc).ToList(),
            replica.NodeIds,
            replica.ResourceGroupName,
            replica.NumOutboundNode.ToDictionary(p => p.Key, p => p.Value));
}

/// <summary>
/// Describes the replica of a single shard (DML channel) of a collection.
/// </summary>
public sealed class MilvusShardReplica
{
    internal MilvusShardReplica(
        long leaderId, string leaderAddress, string dmChannelName, IReadOnlyList<long> nodeIds)
    {
        LeaderId = leaderId;
        LeaderAddress = leaderAddress;
        DmChannelName = dmChannelName;
        NodeIds = nodeIds;
    }

    /// <summary>
    /// The node id of the shard leader.
    /// </summary>
    public long LeaderId { get; }

    /// <summary>
    /// The shard leader's address, as <c>IP:port</c>.
    /// </summary>
    public string LeaderAddress { get; }

    /// <summary>
    /// The DML channel this shard corresponds to.
    /// </summary>
    public string DmChannelName { get; }

    /// <summary>
    /// The nodes serving this shard. Only populated when shard nodes were requested.
    /// </summary>
    public IReadOnlyList<long> NodeIds { get; }

    internal static MilvusShardReplica FromGrpc(Grpc.ShardReplica shard)
        => new(shard.LeaderID, shard.LeaderAddr, shard.DmChannelName, shard.NodeIds);
}
