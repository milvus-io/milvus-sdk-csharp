using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to describe a role, returning its granted privileges and member users.
/// </summary>
public sealed class DescribeRoleReq
{
    /// <summary>
    /// The name of the role to describe.
    /// </summary>
    public string RoleName { get; set; } = "";

    /// <summary>
    /// The database to describe the role in. When empty, the server resolves the <c>default</c> database.
    /// </summary>
    public string? DatabaseName { get; set; }

    internal Grpc.SelectRoleRequest ToGrpcSelectRoleRequest(bool includeUserInfo = true)
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.SelectRoleRequest
        {
            Role = new Grpc.RoleEntity { Name = RoleName },
            IncludeUserInfo = includeUserInfo
        };
    }

    internal Grpc.SelectGrantRequest ToGrpcSelectGrantRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.SelectGrantRequest
        {
            Entity = new Grpc.GrantEntity
            {
                Role = new Grpc.RoleEntity { Name = RoleName },
                DbName = DatabaseName ?? ""
            }
        };
    }
}
