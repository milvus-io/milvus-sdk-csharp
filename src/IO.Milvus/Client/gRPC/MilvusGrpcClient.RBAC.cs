using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task CreateRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        await InvokeAsync(_grpcClient.CreateRoleAsync, new CreateRoleRequest
        {
            Entity = new RoleEntity() { Name = roleName },
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IEnumerable<MilvusRoleResult>> SelectRoleAsync(
        string roleName,
        bool includeUserInfo = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);

        SelectRoleResponse response = await InvokeAsync(_grpcClient.SelectRoleAsync, new SelectRoleRequest
        {
            Role = new RoleEntity() { Name = roleName },
            IncludeUserInfo = includeUserInfo
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusRoleResult.Parse(response.Results);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MilvusUserResult>> SelectUserAsync(
        string username,
        bool includeRoleInfo = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        SelectUserResponse response = await InvokeAsync(_grpcClient.SelectUserAsync, new SelectUserRequest
        {
            User = new UserEntity() { Name = username },
            IncludeRoleInfo = includeRoleInfo
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusUserResult.Parse(response.Results);
    }

    /// <inheritdoc />
    public async Task GrantRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        Verify.NotNullOrWhiteSpace(@object);
        Verify.NotNullOrWhiteSpace(objectName);
        Verify.NotNullOrWhiteSpace(privilege);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.OperatePrivilegeAsync, new OperatePrivilegeRequest
        {
            Type = OperatePrivilegeType.Grant,
            Entity = new GrantEntity()
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName,
                Grantor = new() { Privilege = new() { Name = privilege }, },
                DbName = dbName
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
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
            Entity = new GrantEntity()
            {
                Role = new() { Name = roleName },
                Object = new() { Name = @object },
                ObjectName = objectName,
                Grantor = new() { Privilege = new() { Name = privilege } },
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
