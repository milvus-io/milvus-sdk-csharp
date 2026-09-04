#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class RevokeRoleReq
{
    public string UserName { get; set; } = "";
    public string RoleName { get; set; } = "";
    internal Grpc.OperateUserRoleRequest ToGrpcOperateUserRoleRequest()
    {
        Verify.NotNullOrWhiteSpace(UserName);
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.OperateUserRoleRequest
        {
            Username = UserName,
            RoleName = RoleName,
            Type = Grpc.OperateUserRoleType.RemoveUserFromRole
        };
    }
}
