using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to describe a resource group.
/// </summary>
public sealed class DescribeResourceGroupReq
{
    /// <summary>
    /// The name of the resource group to describe.
    /// </summary>
    public string ResourceGroupName { get; set; } = "";
    internal Grpc.DescribeResourceGroupRequest ToGrpcDescribeResourceGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(ResourceGroupName);
        return new Grpc.DescribeResourceGroupRequest { ResourceGroup = ResourceGroupName };
    }
}
