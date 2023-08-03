using Google.Protobuf.Collections;

namespace Milvus.Client;

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
    public async Task CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(GrpcClient.CreateRoleAsync, new CreateRoleRequest
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
    public async Task DropRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(GrpcClient.DropRoleAsync, new DropRoleRequest
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

        await InvokeAsync(GrpcClient.OperateUserRoleAsync, new OperateUserRoleRequest
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
    public async Task RemoveUserFromRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(GrpcClient.OperateUserRoleAsync, new OperateUserRoleRequest
        {
            RoleName = roleName,
            Username = username,
            Type = OperateUserRoleType.RemoveUserFromRole
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets information about a role, including optionally all its users.
    /// </summary>
    /// <param name="roleName">The name of the role to be selected.</param>
    /// <param name="includeUserInfo">Whether to include user information in the results.</param>
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
    public async Task<MilvusRoleResult?> SelectRoleAsync(
        string roleName,
        bool includeUserInfo = true,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        SelectRoleRequest request = new()
        {
            Role = new RoleEntity { Name = roleName }
        };

        if (includeUserInfo)
        {
            request.IncludeUserInfo = true;
        }

        SelectRoleResponse response =
            await InvokeAsync(GrpcClient.SelectRoleAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        switch (response.Results.Count)
        {
            case 0:
                return null;
            case 1:
                RoleResult result = response.Results[0];
                return new MilvusRoleResult(result.Role.Name, result.Users);
            default:
                throw new InvalidOperationException(
                    $"Unexpected multiple role results returned in {nameof(SelectRoleAsync)}");
        }
    }

    /// <summary>
    /// Gets information about all roles defined in Milvus, including optionally all their users.
    /// </summary>
    /// <param name="includeUserInfo">Whether to include user information in the results.</param>
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
    public async Task<IReadOnlyList<MilvusRoleResult>> SelectAllRolesAsync(
        bool includeUserInfo = true,
        CancellationToken cancellationToken = default)
    {
        SelectRoleRequest request = new();

        if (includeUserInfo)
        {
            request.IncludeUserInfo = true;
        }

        SelectRoleResponse response =
            await InvokeAsync(GrpcClient.SelectRoleAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Results.Select(r => new MilvusRoleResult(r.Role.Name, r.Users)).ToList();
    }

    /// <summary>
    /// Gets information about a user, including optionally all its roles.
    /// </summary>
    /// <param name="username">The name of the user to be selected.</param>
    /// <param name="includeRoleInfo">Whether to include role information in the results.</param>
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
    public async Task<MilvusUserResult?> SelectUserAsync(
        string username,
        bool includeRoleInfo = true,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        SelectUserRequest request = new()
        {
            User = new UserEntity { Name = username }
        };

        if (includeRoleInfo)
        {
            request.IncludeRoleInfo = true;
        }

        SelectUserResponse response =
            await InvokeAsync(GrpcClient.SelectUserAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        switch (response.Results.Count)
        {
            case 0:
                return null;
            case 1:
                UserResult result = response.Results[0];
                return new MilvusUserResult(result.User.Name, result.Roles);
            default:
                throw new InvalidOperationException(
                    $"Unexpected multiple role results returned in {nameof(SelectRoleAsync)}");
        }
    }

    /// <summary>
    /// Gets information about all users defined in Milvus, including optionally all their users.
    /// </summary>
    /// <param name="includeRoleInfo">Whether to include role information in the results.</param>
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
    public async Task<IReadOnlyList<MilvusUserResult>> SelectAllUsersAsync(
        bool includeRoleInfo = true,
        CancellationToken cancellationToken = default)
    {
        SelectUserRequest request = new();

        if (includeRoleInfo)
        {
            request.IncludeRoleInfo = true;
        }

        SelectUserResponse response =
            await InvokeAsync(GrpcClient.SelectUserAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Results.Select(r => new MilvusUserResult(r.User.Name, r.Roles)).ToList();
    }

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

        await InvokeAsync(GrpcClient.OperatePrivilegeAsync, request, cancellationToken).ConfigureAwait(false);
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

        await InvokeAsync(GrpcClient.OperatePrivilegeAsync, request, cancellationToken).ConfigureAwait(false);
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
    public async Task<IReadOnlyList<MilvusGrantEntity>> ListGrantsForRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        SelectGrantResponse response = await InvokeAsync(GrpcClient.SelectGrantAsync, new SelectGrantRequest
        {
            Entity = new() { Role = new() { Name = roleName } }
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Entities
            .Select(e => new MilvusGrantEntity(
                MilvusGrantorEntity.Parse(e.Grantor),
                e.DbName,
                e.Object.Name,
                e.Role.Name,
                e.ObjectName))
            .ToList();
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
    public async Task<IReadOnlyList<MilvusGrantEntity>> SelectGrantForRoleAndObjectAsync(
        string roleName,
        string @object,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        Verify.NotNullOrWhiteSpace(@object);
        Verify.NotNullOrWhiteSpace(objectName);

        SelectGrantResponse response = await InvokeAsync(GrpcClient.SelectGrantAsync, new SelectGrantRequest
        {
            Entity = new()
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName
            }
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Entities
            .Select(e => new MilvusGrantEntity(
                MilvusGrantorEntity.Parse(e.Grantor),
                e.DbName,
                e.Object.Name,
                e.Role.Name,
                e.ObjectName))
            .ToList();
    }
}
