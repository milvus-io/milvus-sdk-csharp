namespace Milvus.Client;

public partial class MilvusDatabase
{
    /// <summary>
    /// Grants a privilege to a role.
    /// </summary>
    /// <param name="roleName">The name of the role to be granted a privilege.</param>
    /// <param name="object">
    /// A string describing the object type on which the privilege is to be granted, e.g. <c>"Collection"</c>.
    /// </param>
    /// <param name="objectName">
    /// A string describing the specific object on which the privilege will be granted. Can be <c>"*"</c>.
    /// </param>
    /// <param name="privilege">A string describing the privilege to be granted, e.g. <c>"Search"</c>.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
    /// </para>
    /// <para>
    /// Roles are available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task GrantRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        Verify.NotNullOrWhiteSpace(@object);
        Verify.NotNullOrWhiteSpace(objectName);
        Verify.NotNullOrWhiteSpace(privilege);

        var request = new OperatePrivilegeRequest
        {
            Type = OperatePrivilegeType.Grant,
            Entity = new GrantEntity
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName,
                Grantor = new() { Privilege = new() { Name = privilege } }
            }
        };

        if (Name is not null)
        {
            request.Entity.DbName = Name;
        }

        await _client.InvokeAsync(_client.GrpcClient.OperatePrivilegeAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Revokes a privilege from a role.
    /// <see href="https://milvus.io/docs/users_and_roles.md"/>
    /// </summary>
    /// <param name="roleName">The name of the role to be revoked a privilege.</param>
    /// <param name="object">
    /// A string describing the object type on which the privilege is to be revoked, e.g. <c>"Collection"</c>.
    /// </param>
    /// <param name="objectName">
    /// A string describing the specific object on which the privilege will be revoked. Can be <c>"*"</c>.
    /// </param>
    /// <param name="privilege">A string describing the privilege to be revoked, e.g. <c>"Search"</c>.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
    /// </para>
    /// <para>
    /// Roles are available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task RevokeRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        Verify.NotNullOrWhiteSpace(@object);
        Verify.NotNullOrWhiteSpace(objectName);
        Verify.NotNullOrWhiteSpace(privilege);

        var request = new OperatePrivilegeRequest
        {
            Type = OperatePrivilegeType.Revoke,
            Entity = new GrantEntity
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName,
                Grantor = new() { Privilege = new() { Name = privilege } },
            }
        };

        if (Name is not null)
        {
            request.Entity.DbName = Name;
        }

        await _client.InvokeAsync(_client.GrpcClient.OperatePrivilegeAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }
}
