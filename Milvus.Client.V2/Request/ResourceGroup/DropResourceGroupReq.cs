using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to drop a resource group.
/// </summary>
public sealed class DropResourceGroupReq
{
    /// <summary>
    /// The name of the resource group to drop.
    /// </summary>
    public string ResourceGroupName { get; set; } = "";
    internal Grpc.DropResourceGroupRequest ToGrpcDropResourceGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(ResourceGroupName);
        return new Grpc.DropResourceGroupRequest { ResourceGroup = ResourceGroupName };
    }
}
