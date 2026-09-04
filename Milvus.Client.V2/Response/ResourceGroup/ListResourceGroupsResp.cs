namespace Milvus.Client.V2.Responses.ResourceGroup;

/// <summary>
/// The result of a list resource groups operation.
/// </summary>
public sealed class ListResourceGroupsResp
{
    internal ListResourceGroupsResp(IReadOnlyList<string> resourceGroups) => ResourceGroups = resourceGroups;
    internal static ListResourceGroupsResp FromGrpc(Grpc.ListResourceGroupsResponse response)
        => new(response.ResourceGroups.ToList());

    /// <summary>
    /// The names of the resource groups.
    /// </summary>
    public IReadOnlyList<string> ResourceGroups { get; }
}
