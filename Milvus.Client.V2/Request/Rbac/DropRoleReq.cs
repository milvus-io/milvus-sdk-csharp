using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to drop a role.
/// </summary>
public sealed class DropRoleReq
{
    /// <summary>
    /// The name of the role to drop.
    /// </summary>
    public string RoleName { get; set; } = "";

    /// <summary>
    /// When true, drops the role even if it still has privilege grants. The server otherwise refuses to drop a
    /// role that has any grants.
    /// </summary>
    public bool ForceDrop { get; set; }

    internal Grpc.DropRoleRequest ToGrpcDropRoleRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.DropRoleRequest { RoleName = RoleName, ForceDrop = ForceDrop };
    }
}
