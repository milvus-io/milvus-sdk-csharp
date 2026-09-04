namespace Milvus.Client.V2.Types;

/// <summary>
/// Information about a shard replica that serves a shard channel of a collection.
/// </summary>
public sealed class ShardReplica
{
    internal ShardReplica(long leaderId, string leaderAddress, string dmChannelName, IReadOnlyList<long> nodeIds)
    {
        LeaderId = leaderId;
        LeaderAddress = leaderAddress;
        DmChannelName = dmChannelName;
        NodeIds = nodeIds;
    }

    /// <summary>
    /// The ID of the query node that leads this shard replica.
    /// </summary>
    public long LeaderId { get; }

    /// <summary>
    /// The address (<c>IP:port</c>) of the leader query node.
    /// </summary>
    public string LeaderAddress { get; }

    /// <summary>
    /// The DM channel name served by this shard replica.
    /// </summary>
    public string DmChannelName { get; }

    /// <summary>
    /// The IDs of the query nodes that host this shard replica, including the leader. Only populated when the
    /// request asks for shard nodes.
    /// </summary>
    public IReadOnlyList<long> NodeIds { get; }

    internal static ShardReplica FromGrpc(Grpc.ShardReplica replica)
        => new(replica.LeaderID, replica.LeaderAddr, replica.DmChannelName, replica.NodeIds.ToList());
}
