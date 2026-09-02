namespace Milvus.Client;

/// <summary>
/// The desired shape of a resource group: how many query nodes it wants, how many it may hold, and
/// which other groups it may exchange nodes with. Available since Milvus v2.4.
/// </summary>
/// <remarks>
/// Milvus continuously reconciles a resource group towards this configuration. If the group holds
/// fewer nodes than <see cref="RequestsNodeNum" />, it pulls nodes in (preferring
/// <see cref="TransferFrom" />); if it holds more than <see cref="LimitsNodeNum" />, it pushes nodes
/// out (preferring <see cref="TransferTo" />).
/// </remarks>
public sealed class ResourceGroupConfig
{
    /// <summary>
    /// Creates a resource group configuration.
    /// </summary>
    /// <param name="requestsNodeNum">
    /// The number of query nodes the group wants. Milvus transfers nodes in from other groups when the
    /// group holds fewer than this.
    /// </param>
    /// <param name="limitsNodeNum">
    /// The maximum number of query nodes the group may hold. Milvus transfers nodes out when the group
    /// holds more than this. Must be greater than or equal to <paramref name="requestsNodeNum" />.
    /// </param>
    /// <param name="transferFrom">Resource groups to take missing nodes from, in priority order.</param>
    /// <param name="transferTo">Resource groups to give redundant nodes to, in priority order.</param>
    /// <param name="nodeLabels">Labels a query node must carry to be placed in this group.</param>
    public ResourceGroupConfig(
        int requestsNodeNum,
        int limitsNodeNum,
        IReadOnlyList<string>? transferFrom = null,
        IReadOnlyList<string>? transferTo = null,
        IReadOnlyDictionary<string, string>? nodeLabels = null)
    {
        Verify.GreaterThanOrEqualTo(requestsNodeNum, 0);
        Verify.GreaterThanOrEqualTo(limitsNodeNum, requestsNodeNum);

        RequestsNodeNum = requestsNodeNum;
        LimitsNodeNum = limitsNodeNum;
        TransferFrom = transferFrom ?? Array.Empty<string>();
        TransferTo = transferTo ?? Array.Empty<string>();
        NodeLabels = nodeLabels ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// The number of query nodes this group wants.
    /// </summary>
    public int RequestsNodeNum { get; }

    /// <summary>
    /// The maximum number of query nodes this group may hold.
    /// </summary>
    public int LimitsNodeNum { get; }

    /// <summary>
    /// Resource groups that missing nodes are taken from, in priority order.
    /// </summary>
    public IReadOnlyList<string> TransferFrom { get; }

    /// <summary>
    /// Resource groups that redundant nodes are given to, in priority order.
    /// </summary>
    public IReadOnlyList<string> TransferTo { get; }

    /// <summary>
    /// Labels a query node must carry to be placed in this group.
    /// </summary>
    public IReadOnlyDictionary<string, string> NodeLabels { get; }

    internal Grpc.ResourceGroupConfig ToGrpc()
    {
        Grpc.ResourceGroupConfig config = new()
        {
            Requests = new Grpc.ResourceGroupLimit { NodeNum = RequestsNodeNum },
            Limits = new Grpc.ResourceGroupLimit { NodeNum = LimitsNodeNum }
        };

        foreach (string name in TransferFrom)
        {
            config.TransferFrom.Add(new Grpc.ResourceGroupTransfer { ResourceGroup = name });
        }

        foreach (string name in TransferTo)
        {
            config.TransferTo.Add(new Grpc.ResourceGroupTransfer { ResourceGroup = name });
        }

        if (NodeLabels.Count > 0)
        {
            config.NodeFilter = new Grpc.ResourceGroupNodeFilter();
            foreach (KeyValuePair<string, string> label in NodeLabels)
            {
                config.NodeFilter.NodeLabels.Add(new Grpc.KeyValuePair { Key = label.Key, Value = label.Value });
            }
        }

        return config;
    }

    internal static ResourceGroupConfig FromGrpc(Grpc.ResourceGroupConfig config)
        => new(
            config.Requests?.NodeNum ?? 0,
            // Guard against a server reporting limits below requests, which the constructor rejects.
            Math.Max(config.Limits?.NodeNum ?? 0, config.Requests?.NodeNum ?? 0),
            config.TransferFrom.Select(t => t.ResourceGroup).ToList(),
            config.TransferTo.Select(t => t.ResourceGroup).ToList(),
            config.NodeFilter?.NodeLabels.ToDictionary(l => l.Key, l => l.Value)
                ?? new Dictionary<string, string>());
}
