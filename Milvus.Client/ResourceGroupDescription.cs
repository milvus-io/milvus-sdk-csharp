namespace Milvus.Client;

/// <summary>
/// The observed state of a resource group. Available since Milvus v2.4.
/// </summary>
public sealed class ResourceGroupDescription
{
    internal ResourceGroupDescription(
        string name,
        int capacity,
        int availableNodeCount,
        IReadOnlyDictionary<string, int> loadedReplicaCounts,
        IReadOnlyDictionary<string, int> outgoingNodeCounts,
        IReadOnlyDictionary<string, int> incomingNodeCounts,
        ResourceGroupConfig config,
        IReadOnlyList<MilvusNodeInfo> nodes)
    {
        Name = name;
        Capacity = capacity;
        AvailableNodeCount = availableNodeCount;
        LoadedReplicaCounts = loadedReplicaCounts;
        OutgoingNodeCounts = outgoingNodeCounts;
        IncomingNodeCounts = incomingNodeCounts;
        Config = config;
        Nodes = nodes;
    }

    /// <summary>
    /// The resource group name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The number of query nodes the group is configured to hold.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// The number of query nodes currently available in the group.
    /// </summary>
    public int AvailableNodeCount { get; }

    /// <summary>
    /// Collection name to the number of its replicas loaded in this group.
    /// </summary>
    public IReadOnlyDictionary<string, int> LoadedReplicaCounts { get; }

    /// <summary>
    /// Collection name to the number of nodes in *other* resource groups that this group's replicas
    /// are accessing.
    /// </summary>
    public IReadOnlyDictionary<string, int> OutgoingNodeCounts { get; }

    /// <summary>
    /// Collection name to the number of this group's nodes being accessed by other resource groups.
    /// </summary>
    public IReadOnlyDictionary<string, int> IncomingNodeCounts { get; }

    /// <summary>
    /// The configuration this group is being reconciled towards.
    /// </summary>
    public ResourceGroupConfig Config { get; }

    /// <summary>
    /// The query nodes currently belonging to this group.
    /// </summary>
    public IReadOnlyList<MilvusNodeInfo> Nodes { get; }

    internal static ResourceGroupDescription FromGrpc(Grpc.ResourceGroup resourceGroup)
        => new(
            resourceGroup.Name,
            resourceGroup.Capacity,
            resourceGroup.NumAvailableNode,
            resourceGroup.NumLoadedReplica.ToDictionary(p => p.Key, p => p.Value),
            resourceGroup.NumOutgoingNode.ToDictionary(p => p.Key, p => p.Value),
            resourceGroup.NumIncomingNode.ToDictionary(p => p.Key, p => p.Value),
            resourceGroup.Config is null
                ? new ResourceGroupConfig(0, 0)
                : ResourceGroupConfig.FromGrpc(resourceGroup.Config),
            resourceGroup.Nodes.Select(MilvusNodeInfo.FromGrpc).ToList());
}

/// <summary>
/// Identifies a Milvus query node.
/// </summary>
public sealed class MilvusNodeInfo
{
    internal MilvusNodeInfo(long nodeId, string address, string hostname)
    {
        NodeId = nodeId;
        Address = address;
        Hostname = hostname;
    }

    /// <summary>
    /// The node id.
    /// </summary>
    public long NodeId { get; }

    /// <summary>
    /// The node address.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// The node hostname.
    /// </summary>
    public string Hostname { get; }

    internal static MilvusNodeInfo FromGrpc(Grpc.NodeInfo nodeInfo)
        => new(nodeInfo.NodeId, nodeInfo.Address, nodeInfo.Hostname);
}
