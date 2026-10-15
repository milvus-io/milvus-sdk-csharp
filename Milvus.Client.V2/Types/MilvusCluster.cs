namespace Milvus.Client.V2.Types;

/// <summary>
/// A Milvus cluster participating in cross-cluster replication.
/// </summary>
public sealed class MilvusCluster
{
    /// <summary>
    /// Creates a new cluster definition.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster.</param>
    /// <param name="uri">The connection URI of the cluster.</param>
    /// <param name="token">The connection token of the cluster.</param>
    /// <param name="pchannels">The physical channels replicated from this cluster.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings",
        Justification = "Deliberately a string to mirror MilvusCluster.uri (proto) and the other Milvus SDKs.")]
    public MilvusCluster(string clusterId, string uri, string token = "", IEnumerable<string>? pchannels = null)
    {
        ClusterId = clusterId;
        Uri = uri;
        Token = token;
        Pchannels = pchannels?.ToList() ?? [];
    }

    /// <summary>
    /// The ID of the cluster.
    /// </summary>
    public string ClusterId { get; }

    /// <summary>
    /// The connection URI of the cluster.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings",
        Justification = "Deliberately a string to mirror MilvusCluster.uri (proto) and the other Milvus SDKs.")]
    public string Uri { get; }

    /// <summary>
    /// The connection token of the cluster.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// The physical channels replicated from this cluster.
    /// </summary>
    public IReadOnlyList<string> Pchannels { get; }

    internal Grpc.MilvusCluster ToGrpc()
    {
        var cluster = new Grpc.MilvusCluster
        {
            ClusterId = ClusterId,
            ConnectionParam = new Grpc.ConnectionParam { Uri = Uri, Token = Token },
        };
        cluster.Pchannels.AddRange(Pchannels);
        return cluster;
    }

    internal static MilvusCluster FromGrpc(Grpc.MilvusCluster cluster)
        => new(
            cluster.ClusterId,
            cluster.ConnectionParam?.Uri ?? "",
            cluster.ConnectionParam?.Token ?? "",
            cluster.Pchannels);
}
