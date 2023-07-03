using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    /// <inheritdoc />
    public Task CreateRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task DropRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task AddUserToRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task RemoveUserFromRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task<IEnumerable<MilvusRoleResult>> SelectRoleAsync(
        string roleName,
        bool includeUserInfo = false,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task<IEnumerable<MilvusUserResult>> SelectUserAsync(
        string username,
        bool includeRoleInfo = false,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task GrantRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege,
        string dbName = "default",
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task RevokeRolePrivilegeAsync(
        string roleName,
        string @object,
        string objectName,
        string privilege, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <inheritdoc />
    public Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAndObjectAsync(
        string roleName,
        string @object,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }
}