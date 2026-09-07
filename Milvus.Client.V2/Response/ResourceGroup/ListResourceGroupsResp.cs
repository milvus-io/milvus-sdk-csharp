#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.ResourceGroup;
public sealed class ListResourceGroupsResp
{
    internal ListResourceGroupsResp(IReadOnlyList<string> resourceGroups) => ResourceGroups = resourceGroups;
    internal static ListResourceGroupsResp FromGrpc(Grpc.ListResourceGroupsResponse response)
        => new(response.ResourceGroups.ToList());
    public IReadOnlyList<string> ResourceGroups { get; }
}
