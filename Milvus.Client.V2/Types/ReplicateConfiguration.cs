namespace Milvus.Client.V2.Types;

/// <summary>
/// The replication configuration of a Milvus cluster.
/// </summary>
public sealed class ReplicateConfiguration
{
    /// <summary>
    /// Creates a new replication configuration.
    /// </summary>
    /// <param name="clusters">The clusters participating in replication.</param>
    /// <param name="crossClusterTopologies">The cross-cluster topology entries.</param>
    public ReplicateConfiguration(
        IEnumerable<MilvusCluster> clusters,
        IEnumerable<CrossClusterTopology>? crossClusterTopologies = null)
    {
        Clusters = clusters.ToList();
        CrossClusterTopologies = crossClusterTopologies?.ToList() ?? [];
    }

    /// <summary>
    /// The clusters participating in replication.
    /// </summary>
    public IReadOnlyList<MilvusCluster> Clusters { get; }

    /// <summary>
    /// The cross-cluster topology entries.
    /// </summary>
    public IReadOnlyList<CrossClusterTopology> CrossClusterTopologies { get; }

    internal Grpc.ReplicateConfiguration ToGrpc()
    {
        var configuration = new Grpc.ReplicateConfiguration();
        configuration.Clusters.AddRange(Clusters.Select(c => c.ToGrpc()));
        configuration.CrossClusterTopology.AddRange(CrossClusterTopologies.Select(t => t.ToGrpc()));
        return configuration;
    }

    internal static ReplicateConfiguration FromGrpc(Grpc.ReplicateConfiguration configuration)
        => new(
            configuration.Clusters.Select(MilvusCluster.FromGrpc),
            configuration.CrossClusterTopology.Select(CrossClusterTopology.FromGrpc));
}
