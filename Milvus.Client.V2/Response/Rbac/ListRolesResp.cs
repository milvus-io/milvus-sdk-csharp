#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class ListRolesResp
{
    internal ListRolesResp(IReadOnlyList<RoleResult> roles) => Roles = roles;
    internal static ListRolesResp FromGrpc(Grpc.SelectRoleResponse response)
        => new(response.Results.Select(RoleResult.FromGrpc).ToList());
    public IReadOnlyList<RoleResult> Roles { get; }
}
