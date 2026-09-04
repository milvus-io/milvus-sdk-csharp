using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Responses.ResourceGroup;

/// <summary>
/// The result of a describe resource group operation.
/// </summary>
public sealed class DescribeResourceGroupResp
{
    internal DescribeResourceGroupResp(
        string name,
        int capacity,
        int numAvailableNode,
        IReadOnlyDictionary<string, int> numLoadedReplica,
        IReadOnlyDictionary<string, int> numOutgoingNode,
        IReadOnlyDictionary<string, int> numIncomingNode,
        ResourceGroupConfigData config,
        IReadOnlyList<NodeInfoData> nodes)
    {
        Name = name;
        Capacity = capacity;
        NumAvailableNode = numAvailableNode;
        NumLoadedReplica = numLoadedReplica;
        NumOutgoingNode = numOutgoingNode;
        NumIncomingNode = numIncomingNode;
        Config = config;
        Nodes = nodes;
    }

    internal static DescribeResourceGroupResp FromGrpc(Grpc.DescribeResourceGroupResponse response)
    {
        // ResourceGroup is a singular proto message; it may be unset (null) on servers that don't populate it.
        Milvus.Client.Grpc.ResourceGroup? group = response.ResourceGroup;
        return new DescribeResourceGroupResp(
            group?.Name ?? "",
            group?.Capacity ?? 0,
            group?.NumAvailableNode ?? 0,
            group?.NumLoadedReplica.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>(),
            group?.NumOutgoingNode.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>(),
            group?.NumIncomingNode.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>(),
            ResourceGroupConfigData.FromGrpc(group?.Config),
            group?.Nodes.Select(NodeInfoData.FromGrpc).ToList() ?? []);
    }

    /// <summary>
    /// The name of the resource group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The number of query nodes in the resource group.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// The number of available query nodes in the resource group.
    /// </summary>
    public int NumAvailableNode { get; }

    /// <summary>
    /// The number of loaded replicas per collection in the resource group.
    /// </summary>
    public IReadOnlyDictionary<string, int> NumLoadedReplica { get; }

    /// <summary>
    /// The number of nodes outgoing to other resource groups, keyed by collection name (per the wire
    /// protocol: "collection name -&gt; accessed other rg's node num").
    /// </summary>
    public IReadOnlyDictionary<string, int> NumOutgoingNode { get; }

    /// <summary>
    /// The number of nodes incoming from other resource groups, keyed by collection name (per the wire
    /// protocol: "collection name -&gt; be accessed node num by other rg").
    /// </summary>
    public IReadOnlyDictionary<string, int> NumIncomingNode { get; }

    /// <summary>
    /// The configuration of the resource group.
    /// </summary>
    public ResourceGroupConfigData Config { get; }

    /// <summary>
    /// The query nodes in the resource group.
    /// </summary>
    public IReadOnlyList<NodeInfoData> Nodes { get; }
}

/// <summary>
/// The configuration data of a resource group.
/// </summary>
public sealed class ResourceGroupConfigData
{
    internal ResourceGroupConfigData(
        int? requestsNodeNum,
        int? limitsNodeNum,
        IReadOnlyList<string> transferFrom,
        IReadOnlyList<string> transferTo,
        IReadOnlyList<KeyValuePair<string, string>> nodeFilterLabels)
    {
        RequestsNodeNum = requestsNodeNum;
        LimitsNodeNum = limitsNodeNum;
        TransferFrom = transferFrom;
        TransferTo = transferTo;
        NodeFilterLabels = nodeFilterLabels;
    }

    internal static ResourceGroupConfigData FromGrpc(ResourceGroupConfig? config)
        => new(
            config?.Requests is null ? null : config.Requests.NodeNum,
            config?.Limits is null ? null : config.Limits.NodeNum,
            config?.TransferFrom.Select(t => t.ResourceGroup).ToList() ?? [],
            config?.TransferTo.Select(t => t.ResourceGroup).ToList() ?? [],
            config?.NodeFilter?.NodeLabels.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToList()
                ?? []);

    /// <summary>
    /// The requested number of query nodes for the resource group, or <c>null</c> if not set.
    /// </summary>
    public int? RequestsNodeNum { get; }

    /// <summary>
    /// The maximum number of query nodes for the resource group, or <c>null</c> if not set.
    /// </summary>
    public int? LimitsNodeNum { get; }

    /// <summary>
    /// The names of the resource groups that can have nodes transferred from.
    /// </summary>
    public IReadOnlyList<string> TransferFrom { get; }

    /// <summary>
    /// The names of the resource groups that can have nodes transferred to.
    /// </summary>
    public IReadOnlyList<string> TransferTo { get; }

    /// <summary>
    /// The node filter labels used to match query nodes for the resource group.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> NodeFilterLabels { get; }
}

/// <summary>
/// The information of a query node in a resource group.
/// </summary>
public sealed class NodeInfoData
{
    internal NodeInfoData(long nodeId, string address, string hostname)
    {
        NodeId = nodeId;
        Address = address;
        Hostname = hostname;
    }

    internal static NodeInfoData FromGrpc(NodeInfo node)
        => new(node.NodeId, node.Address, node.Hostname);

    /// <summary>
    /// The ID of the query node.
    /// </summary>
    public long NodeId { get; }

    /// <summary>
    /// The address of the query node.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// The hostname of the query node.
    /// </summary>
    public string Hostname { get; }
}
