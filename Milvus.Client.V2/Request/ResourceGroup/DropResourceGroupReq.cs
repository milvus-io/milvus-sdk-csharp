#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;
public sealed class DropResourceGroupReq
{
    public string ResourceGroupName { get; set; } = "";
    internal Grpc.DropResourceGroupRequest ToGrpcDropResourceGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(ResourceGroupName);
        return new Grpc.DropResourceGroupRequest { ResourceGroup = ResourceGroupName };
    }
}
