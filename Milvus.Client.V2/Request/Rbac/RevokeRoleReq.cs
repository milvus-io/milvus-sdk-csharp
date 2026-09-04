using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to revoke a role from a user.
/// </summary>
public sealed class RevokeRoleReq
{
    /// <summary>
    /// The name of the user to revoke the role from.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The name of the role to revoke.
    /// </summary>
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
