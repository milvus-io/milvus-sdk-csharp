using IO.Milvus.Grpc;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Create a role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name that will be created.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task CreateRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(_grpcClient.CreateRoleAsync, new CreateRoleRequest
        {
            Entity = new RoleEntity { Name = roleName }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drop a role.
    /// </summary>
    /// <remarks>
    ///  <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName"></param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task DropRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(_grpcClient.DropRoleAsync, new DropRoleRequest
        {
            RoleName = roleName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Add user to role.
    /// </summary>
    /// <remarks>
    ///<para>
    /// The user will get permissions that the role are allowed to perform operations.
    ///</para>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="username">Username.</param>
    /// <param name="roleName">Role name.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task AddUserToRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(_grpcClient.OperateUserRoleAsync, new OperateUserRoleRequest
        {
            RoleName = roleName,
            Username = username,
            Type = OperateUserRoleType.AddUserToRole
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove user from role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The user will remove permissions that the role are allowed to perform operations.
    /// </para>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="username">Username.</param>
    /// <param name="roleName">RoleName.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task RemoveUserFromRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(_grpcClient.OperateUserRoleAsync, new OperateUserRoleRequest
        {
            RoleName = roleName,
            Username = username,
            Type = OperateUserRoleType.RemoveUserFromRole
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get all users who are added to the role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name.</param>
    /// <param name="includeUserInfo">Include user information.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusRoleResult>> SelectRoleAsync(
        string roleName,
        bool includeUserInfo = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        SelectRoleResponse response = await InvokeAsync(_grpcClient.SelectRoleAsync, new SelectRoleRequest
        {
            Role = new RoleEntity { Name = roleName },
            IncludeUserInfo = includeUserInfo
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusRoleResult.Parse(response.Results);
    }

    /// <summary>
    /// Get all roles the user has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="username">User name.</param>
    /// <param name="includeRoleInfo">Include user information</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusUserResult>> SelectUserAsync(
        string username,
        bool includeRoleInfo = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        SelectUserResponse response = await InvokeAsync(_grpcClient.SelectUserAsync, new SelectUserRequest
        {
            User = new UserEntity { Name = username },
            IncludeRoleInfo = includeRoleInfo
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusUserResult.Parse(response.Results);
    }

    /// <summary>
    /// Grant Role Privilege.
    /// <see href="https://milvus.io/docs/users_and_roles.md"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name. </param>
    /// <param name="object">object.</param>
    /// <param name="objectName">object name.</param>
    /// <param name="privilege">privilege.</param>
    /// <param name="dbName">Database name.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    public async Task GrantRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        string? dbName = null,
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

        if (dbName is not null)
        {
            request.Entity.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.OperatePrivilegeAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Revoke Role Privilege.
    /// <see href="https://milvus.io/docs/users_and_roles.md"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name. </param>
    /// <param name="object">object.</param>
    /// <param name="objectName">object name.</param>
    /// <param name="privilege">privilege.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
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

        await InvokeAsync(_grpcClient.OperatePrivilegeAsync, new OperatePrivilegeRequest
        {
            Type = OperatePrivilegeType.Revoke,
            Entity = new GrantEntity
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName,
                Grantor = new() { Privilege = new() { Name = privilege } },
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///  List a grant info for the role and the specific object
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName">Role name. RoleName cannot be empty or null.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        SelectGrantResponse response = await InvokeAsync(_grpcClient.SelectGrantAsync, new SelectGrantRequest
        {
            Entity = new() { Role = new() { Name = roleName } }
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusGrantEntity.Parse(response.Entities);
    }

    /// <summary>
    /// List a grant info for the role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="roleName">RoleName cannot be empty or null.</param>
    /// <param name="object">object. object cannot be empty or null.</param>
    /// <param name="objectName">objectName. objectName cannot be empty or null.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    public async Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAndObjectAsync(
        string roleName,
        string @object,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        Verify.NotNullOrWhiteSpace(@object);
        Verify.NotNullOrWhiteSpace(objectName);

        SelectGrantResponse response = await InvokeAsync(_grpcClient.SelectGrantAsync, new SelectGrantRequest
        {
            Entity = new()
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName
            }
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusGrantEntity.Parse(response.Entities);
    }
}
