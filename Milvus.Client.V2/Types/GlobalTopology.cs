namespace Milvus.Client.V2.Types;

/// <summary>
/// Capability bit flags describing what a member cluster of a global-cluster (Zilliz Cloud) deployment can serve.
/// Mirrors the <c>ClusterCapability</c> of the Java/C++ SDKs.
/// </summary>
public static class ClusterCapability
{
    /// <summary>
    /// The cluster can serve read requests.
    /// </summary>
    public const int Readable = 0b01;

    /// <summary>
    /// The cluster can serve write requests.
    /// </summary>
    public const int Writable = 0b10;

    /// <summary>
    /// The cluster is the primary, capable of both reads and writes.
    /// </summary>
    public const int Primary = Readable | Writable; // 0b11
}

/// <summary>
/// A single member cluster in a global-cluster topology.
/// </summary>
/// <remarks>
/// Zilliz Cloud only. Mirrors the <c>ClusterInfo</c> of the Java/C++ SDKs.
/// </remarks>
public sealed class ClusterInfo
{
    internal ClusterInfo(string clusterId, string endpoint, int capability)
    {
        ClusterId = clusterId;
        Endpoint = endpoint;
        Capability = capability;
    }

    /// <summary>
    /// The identifier of the cluster.
    /// </summary>
    public string ClusterId { get; }

    /// <summary>
    /// The gRPC endpoint of the cluster (host:port).
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// The capability bit flags of the cluster; see <see cref="ClusterCapability" />.
    /// </summary>
    public int Capability { get; }

    /// <summary>
    /// Whether this cluster is the primary (writable) cluster.
    /// </summary>
    public bool IsPrimary => (Capability & ClusterCapability.Writable) != 0;
}

/// <summary>
/// The global-cluster topology discovered from a global endpoint, identifying the member clusters and their
/// capabilities. The primary (writable) cluster is the one gRPC calls are routed to.
/// </summary>
/// <remarks>
/// Zilliz Cloud only. Mirrors the <c>GlobalTopology</c> of the Java/C++ SDKs.
/// </remarks>
public sealed class GlobalTopology
{
    internal GlobalTopology(long version, IReadOnlyList<ClusterInfo> clusters)
    {
        Version = version;
        Clusters = clusters;
    }

    /// <summary>
    /// The topology version, used to discard stale refresh answers.
    /// </summary>
    public long Version { get; }

    /// <summary>
    /// The member clusters of the deployment.
    /// </summary>
    public IReadOnlyList<ClusterInfo> Clusters { get; }

    /// <summary>
    /// The primary (writable) cluster of the topology.
    /// </summary>
    /// <exception cref="InvalidOperationException">When no cluster is marked writable.</exception>
    public ClusterInfo Primary
        => Clusters.FirstOrDefault(c => c.IsPrimary)
            ?? throw new InvalidOperationException("No primary (writable) cluster found in global topology");
}
