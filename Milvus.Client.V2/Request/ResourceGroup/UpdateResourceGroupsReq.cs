using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to update the configuration of multiple resource groups.
/// </summary>
public sealed class UpdateResourceGroupsReq
{
    /// <summary>
    /// The resource group names and their updated configurations.
    /// </summary>
    public IReadOnlyDictionary<string, ResourceGroupConfig> ResourceGroups { get; set; } =
        new Dictionary<string, ResourceGroupConfig>();
    internal Grpc.UpdateResourceGroupsRequest ToGrpcUpdateResourceGroupsRequest()
    {
        Verify.NotNull(ResourceGroups);
        Verify.NotNullOrEmpty(ResourceGroups.Keys.ToList());
        var request = new Grpc.UpdateResourceGroupsRequest();
        foreach (KeyValuePair<string, ResourceGroupConfig> entry in ResourceGroups)
        {
            Verify.NotNull(entry.Value);
            request.ResourceGroups.Add(entry.Key, entry.Value.ToGrpc());
        }
        return request;
    }
}
