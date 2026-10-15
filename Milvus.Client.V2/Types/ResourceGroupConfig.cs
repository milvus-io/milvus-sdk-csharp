namespace Milvus.Client.V2.Types;

/// <summary>
/// The configuration of a resource group, mirroring the C++ <c>ResourceGroupConfig</c> and the Java
/// <c>ResourceGroupConfig</c>.
/// </summary>
public sealed class ResourceGroupConfig
{
    /// <summary>
    /// The number of nodes requested for the resource group.
    /// </summary>
    public int? RequestsNodeNum { get; set; }

    /// <summary>
    /// The maximum number of nodes the resource group may use.
    /// </summary>
    public int? LimitsNodeNum { get; set; }

    /// <summary>
    /// The resource groups from which nodes are transferred into this resource group.
    /// </summary>
    public IList<string> TransferFrom { get; } = new List<string>();

    /// <summary>
    /// The resource groups to which nodes are transferred from this resource group.
    /// </summary>
    public IList<string> TransferTo { get; } = new List<string>();

    /// <summary>
    /// The node labels used to filter nodes when assigning them to this resource group.
    /// </summary>
    public IDictionary<string, string> NodeFilterLabels { get; } = new Dictionary<string, string>();

    internal Milvus.Client.Grpc.ResourceGroupConfig ToGrpc()
    {
        var result = new Milvus.Client.Grpc.ResourceGroupConfig();

        if (RequestsNodeNum is { } requests)
        {
            result.Requests = new Milvus.Client.Grpc.ResourceGroupLimit { NodeNum = requests };
        }

        if (LimitsNodeNum is { } limits)
        {
            result.Limits = new Milvus.Client.Grpc.ResourceGroupLimit { NodeNum = limits };
        }

        foreach (string group in TransferFrom)
        {
            result.TransferFrom.Add(new Milvus.Client.Grpc.ResourceGroupTransfer { ResourceGroup = group });
        }

        foreach (string group in TransferTo)
        {
            result.TransferTo.Add(new Milvus.Client.Grpc.ResourceGroupTransfer { ResourceGroup = group });
        }

        if (NodeFilterLabels.Count > 0)
        {
            var filter = new Milvus.Client.Grpc.ResourceGroupNodeFilter();
            foreach (KeyValuePair<string, string> label in NodeFilterLabels)
            {
                filter.NodeLabels.Add(new Milvus.Client.Grpc.KeyValuePair { Key = label.Key, Value = label.Value });
            }

            result.NodeFilter = filter;
        }

        return result;
    }
}
