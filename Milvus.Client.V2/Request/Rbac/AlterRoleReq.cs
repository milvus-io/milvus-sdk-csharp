using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to alter a role's remark.
/// </summary>
public sealed class AlterRoleReq
{
    /// <summary>
    /// The name of the role to alter.
    /// </summary>
    public string RoleName { get; set; } = "";

    /// <summary>
    /// The new description of the role.
    /// </summary>
    public string Description { get; set; } = "";

    internal Grpc.AlterRoleRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);

        return new Grpc.AlterRoleRequest
        {
            RoleName = RoleName,
            Description = Description
        };
    }
}
