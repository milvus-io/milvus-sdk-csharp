#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class ListPrivilegeGroupsResp
{
    internal ListPrivilegeGroupsResp(IReadOnlyList<string> groupNames) => GroupNames = groupNames;
    internal static ListPrivilegeGroupsResp FromGrpc(Grpc.ListPrivilegeGroupsResponse response)
        => new(response.PrivilegeGroups.Select(g => g.GroupName).ToList());
    public IReadOnlyList<string> GroupNames { get; }
}
