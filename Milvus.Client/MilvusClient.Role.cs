namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates a role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
    /// </para>
    /// <para>
    /// Roles are available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    /// <param name="roleName">The name of the role to be created.</param>
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
    /// Drops a role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
    /// </para>
    /// <para>
    /// Roles are available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    /// <param name="roleName">The name of the role to be dropped.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task DropRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(GrpcClient.DropRoleAsync, new DropRoleRequest
        {
            RoleName = roleName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a user to a role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
    /// </para>
    /// <para>
    /// Roles are available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    /// <param name="username">The name of the username to be added to the role.</param>
    /// <param name="roleName">The name of the role the user will be added to.</param>
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
    /// Removes a user from a role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
    /// </para>
    /// <para>
    /// Roles are available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    /// <param name="username">The name of the user to be removed from the role.</param>
    /// <param name="roleName">The name of the role from which the user is to be removed.</param>
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
    /// <returns>
    /// A <see cref="RoleResult" /> instance containing information about the role, or <c>null</c> if the role does not
    /// exist.
    /// </returns>
    public async Task<RoleResult?> SelectRoleAsync(
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
                Grpc.RoleResult result = response.Results[0];
                return new RoleResult(result.Role.Name, result.Users);
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
    /// <returns>A list of <see cref="RoleResult" /> instances containing information about all the roles.</returns>
    public async Task<IReadOnlyList<RoleResult>> SelectAllRolesAsync(
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

        return response.Results.Select(r => new RoleResult(r.Role.Name, r.Users)).ToList();
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
    /// <returns>
    /// A <see cref="UserResult" /> instance containing information about the user, or <c>null</c> if the user does not
    /// exist.
    /// </returns>
    public async Task<UserResult?> SelectUserAsync(
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
                Grpc.UserResult result = response.Results[0];
                return new UserResult(result.User.Name, result.Roles);
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
    /// <returns>A list of <see cref="UserResult" /> instances containing information about all the users.</returns>
    public async Task<IReadOnlyList<UserResult>> SelectAllUsersAsync(
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

        return response.Results.Select(r => new UserResult(r.User.Name, r.Roles)).ToList();
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
            Entity = new Grpc.GrantEntity
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
            Entity = new Grpc.GrantEntity
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
    /// Lists a grant info for the role and the specific object.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
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
    /// <returns>A list of <see cref="GrantEntity" /> instances describing the grants assigned to the role.</returns>
    public async Task<IReadOnlyList<GrantEntity>> ListGrantsForRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        SelectGrantResponse response = await InvokeAsync(GrpcClient.SelectGrantAsync, new SelectGrantRequest
        {
            Entity = new() { Role = new() { Name = roleName } }
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Entities
            .Select(e => new GrantEntity(
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
    public async Task<IReadOnlyList<GrantEntity>> SelectGrantForRoleAndObjectAsync(
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
            .Select(e => new GrantEntity(
                MilvusGrantorEntity.Parse(e.Grantor),
                e.DbName,
                e.Object.Name,
                e.Role.Name,
                e.ObjectName))
            .ToList();
    }
}
