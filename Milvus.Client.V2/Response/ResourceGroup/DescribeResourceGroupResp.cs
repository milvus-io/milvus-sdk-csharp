#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Responses.ResourceGroup;

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
        Milvus.Client.Grpc.ResourceGroup group = response.ResourceGroup;
        return new DescribeResourceGroupResp(
            group.Name,
            group.Capacity,
            group.NumAvailableNode,
            group.NumLoadedReplica.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            group.NumOutgoingNode.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            group.NumIncomingNode.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ResourceGroupConfigData.FromGrpc(group.Config),
            group.Nodes.Select(NodeInfoData.FromGrpc).ToList());
    }

    public string Name { get; }
    public int Capacity { get; }
    public int NumAvailableNode { get; }
    public IReadOnlyDictionary<string, int> NumLoadedReplica { get; }
    public IReadOnlyDictionary<string, int> NumOutgoingNode { get; }
    public IReadOnlyDictionary<string, int> NumIncomingNode { get; }
    public ResourceGroupConfigData Config { get; }
    public IReadOnlyList<NodeInfoData> Nodes { get; }
}

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

    internal static ResourceGroupConfigData FromGrpc(ResourceGroupConfig config)
        => new(
            config?.Requests is null ? null : config.Requests.NodeNum,
            config?.Limits is null ? null : config.Limits.NodeNum,
            config?.TransferFrom.Select(t => t.ResourceGroup).ToList() ?? [],
            config?.TransferTo.Select(t => t.ResourceGroup).ToList() ?? [],
            config?.NodeFilter?.NodeLabels.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToList()
                ?? []);

    public int? RequestsNodeNum { get; }
    public int? LimitsNodeNum { get; }
    public IReadOnlyList<string> TransferFrom { get; }
    public IReadOnlyList<string> TransferTo { get; }
    public IReadOnlyList<KeyValuePair<string, string>> NodeFilterLabels { get; }
}

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

    public long NodeId { get; }
    public string Address { get; }
    public string Hostname { get; }
}
