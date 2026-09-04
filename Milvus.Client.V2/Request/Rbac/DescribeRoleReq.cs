#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class DescribeRoleReq
{
    public string RoleName { get; set; } = "";
    internal Grpc.SelectRoleRequest ToGrpcSelectRoleRequest(bool includeUserInfo = true)
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.SelectRoleRequest
        {
            Role = new Grpc.RoleEntity { Name = RoleName },
            IncludeUserInfo = includeUserInfo
        };
    }
}
