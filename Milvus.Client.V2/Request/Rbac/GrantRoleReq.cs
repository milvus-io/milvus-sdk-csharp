using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to grant a role to a user.
/// </summary>
public sealed class GrantRoleReq
{
    /// <summary>
    /// The name of the user to grant the role to.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The name of the role to grant.
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
            Type = Grpc.OperateUserRoleType.AddUserToRole
        };
    }
}
