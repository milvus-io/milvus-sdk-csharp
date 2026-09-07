#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class DescribeRoleResp
{
    internal DescribeRoleResp(RoleResult? role) => Role = role;
    internal static DescribeRoleResp FromGrpc(Grpc.SelectRoleResponse response)
    {
        RoleResult? role = response.Results.Count == 0 ? null : RoleResult.FromGrpc(response.Results[0]);
        return new DescribeRoleResp(role);
    }
    public RoleResult? Role { get; }
}
