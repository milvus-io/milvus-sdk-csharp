using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to grant a privilege to a role using the v2 object/privilege model.
/// </summary>
public sealed class GrantPrivilegeReqV2
{
    /// <summary>
    /// The name of the role to which the privilege is granted.
    /// </summary>
    public string RoleName { get; set; } = "";

    /// <summary>
    /// The privilege to grant, e.g. <c>"Search"</c> or <c>"CreateIndex"</c>.
    /// </summary>
    public string Privilege { get; set; } = "";

    /// <summary>
    /// The name of the database the privilege applies to.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>
    /// The name of the collection the privilege applies to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.OperatePrivilegeV2Request ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        Verify.NotNullOrWhiteSpace(Privilege);

        return new Grpc.OperatePrivilegeV2Request
        {
            Role = new Grpc.RoleEntity { Name = RoleName },
            Grantor = new Grpc.GrantorEntity
            {
                Privilege = new Grpc.PrivilegeEntity { Name = Privilege }
            },
            DbName = DatabaseName,
            CollectionName = CollectionName,
            Type = Grpc.OperatePrivilegeType.Grant
        };
    }
}
