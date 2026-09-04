using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to revoke a privilege on an object from a role.
/// </summary>
public sealed class RevokePrivilegeReq
{
    /// <summary>
    /// The name of the role to revoke the privilege from.
    /// </summary>
    public string RoleName { get; set; } = "";

    /// <summary>
    /// The type of the object to revoke the privilege on (e.g. <c>Collection</c>, <c>Global</c>).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Property name mirrors the Milvus 'object' RPC field.")]
    public string Object { get; set; } = "";

    /// <summary>
    /// The name of the object to revoke the privilege on.
    /// </summary>
    public string ObjectName { get; set; } = "";

    /// <summary>
    /// The privilege to revoke (e.g. <c>Search</c>, <c>Query</c>, <c>Insert</c>).
    /// </summary>
    public string Privilege { get; set; } = "";

    /// <summary>
    /// The database in which the object is defined. When empty, the server resolves the <c>default</c> database.
    /// </summary>
    public string? DatabaseName { get; set; }

    internal Grpc.OperatePrivilegeRequest ToGrpcOperatePrivilegeRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        Verify.NotNullOrWhiteSpace(Privilege);
        return new Grpc.OperatePrivilegeRequest
        {
            Entity = new Grpc.GrantEntity
            {
                Role = new Grpc.RoleEntity { Name = RoleName },
                Object = new Grpc.ObjectEntity { Name = Object },
                ObjectName = ObjectName,
                DbName = DatabaseName ?? "",
                Grantor = new Grpc.GrantorEntity
                {
                    Privilege = new Grpc.PrivilegeEntity { Name = Privilege }
                }
            },
            Type = Grpc.OperatePrivilegeType.Revoke
        };
    }
}
