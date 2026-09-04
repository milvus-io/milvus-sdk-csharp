namespace Milvus.Client.V2.Types;

/// <summary>
/// A cross-cluster topology entry describing a replication direction between two clusters.
/// </summary>
public sealed class CrossClusterTopology
{
    /// <summary>
    /// Creates a new topology entry.
    /// </summary>
    /// <param name="sourceClusterId">The ID of the source cluster.</param>
    /// <param name="targetClusterId">The ID of the target cluster.</param>
    public CrossClusterTopology(string sourceClusterId, string targetClusterId)
    {
        SourceClusterId = sourceClusterId;
        TargetClusterId = targetClusterId;
    }

    /// <summary>
    /// The ID of the source cluster.
    /// </summary>
    public string SourceClusterId { get; }

    /// <summary>
    /// The ID of the target cluster.
    /// </summary>
    public string TargetClusterId { get; }

    internal Grpc.CrossClusterTopology ToGrpc()
        => new()
        {
            SourceClusterId = SourceClusterId,
            TargetClusterId = TargetClusterId
        };

    internal static CrossClusterTopology FromGrpc(Grpc.CrossClusterTopology topology)
        => new(topology.SourceClusterId, topology.TargetClusterId);
}
