using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to create a role.
/// </summary>
public sealed class CreateRoleReq
{
    /// <summary>
    /// The name of the role to create.
    /// </summary>
    public string RoleName { get; set; } = "";

    /// <summary>
    /// An optional description for the role.
    /// </summary>
    public string? Description { get; set; }

    internal Grpc.CreateRoleRequest ToGrpcCreateRoleRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.CreateRoleRequest { Entity = new Grpc.RoleEntity { Name = RoleName, Description = Description ?? "" } };
    }
}
