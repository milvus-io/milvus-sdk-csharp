using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to create a resource group.
/// </summary>
public sealed class CreateResourceGroupReq
{
    /// <summary>
    /// The name of the resource group to create.
    /// </summary>
    public string ResourceGroupName { get; set; } = "";

    /// <summary>
    /// The optional resource group configuration applied at creation time.
    /// </summary>
    public ResourceGroupConfig? Config { get; set; }

    internal Grpc.CreateResourceGroupRequest ToGrpcCreateResourceGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(ResourceGroupName);
        var request = new Grpc.CreateResourceGroupRequest { ResourceGroup = ResourceGroupName };
        if (Config is not null)
        {
            request.Config = Config.ToGrpc();
        }

        return request;
    }
}
