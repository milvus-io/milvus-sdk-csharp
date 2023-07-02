using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task CreateRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        _log.LogDebug("Creating role {0}.", roleName);

        Grpc.Status response = await _grpcClient.CreateRoleAsync(new Grpc.CreateRoleRequest()
        {
            Entity = new Grpc.RoleEntity()
            {
                Name = roleName
            }
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Drop role failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task DropRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        _log.LogDebug("Drop role {0}.", roleName);

        Grpc.Status response = await _grpcClient.DropRoleAsync(new Grpc.DropRoleRequest()
        {
            RoleName = roleName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Drop role failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task AddUserToRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(roleName);
        _log.LogDebug("Add user to role {0} {1}.", username, roleName);

        Grpc.Status response = await _grpcClient.OperateUserRoleAsync(new Grpc.OperateUserRoleRequest()
        {
            RoleName = roleName,
            Username = username,
            Type = Grpc.OperateUserRoleType.AddUserToRole
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed add user to role {0} {1}.", username, roleName);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task RemoveUserFromRoleAsync(
        string username,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(roleName);
        _log.LogDebug("Remove user to role {0} {1}.", username, roleName);

        Grpc.Status response = await _grpcClient.OperateUserRoleAsync(new Grpc.OperateUserRoleRequest()
        {
            RoleName = roleName,
            Username = username,
            Type = Grpc.OperateUserRoleType.RemoveUserFromRole
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed Remove user to role {0} {1}.", username, roleName);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusRoleResult>> SelectRoleAsync(
        string roleName,
        bool includeUserInfo = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        _log.LogDebug("Select role {0}, includeUserInfo: {1}.", roleName, includeUserInfo);

        Grpc.SelectRoleResponse response = await _grpcClient.SelectRoleAsync(new Grpc.SelectRoleRequest()
        {
            Role = new Grpc.RoleEntity()
            {
                Name = roleName
            },
            IncludeUserInfo = includeUserInfo
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed select role {0}, includeUserInfo: {1}.", roleName, includeUserInfo);
            throw new MilvusException(response.Status);
        }

        return MilvusRoleResult.Parse(response.Results);
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusUserResult>> SelectUserAsync(
        string username,
        bool includeRoleInfo = false,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        _log.LogDebug("Select user {0}, includeRoleInfo: {1}.", username, includeRoleInfo);

        Grpc.SelectUserResponse response = await _grpcClient.SelectUserAsync(new Grpc.SelectUserRequest()
        {
            User = new Grpc.UserEntity()
            {
                Name = username
            },
            IncludeRoleInfo = includeRoleInfo
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed select user {0}, includeRoleInfo: {1}.", username, includeRoleInfo);
            throw new MilvusException(response.Status);
        }

        return MilvusUserResult.Parse(response.Results);
    }

    ///<inheritdoc/>
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

        _log.LogDebug("Grant role {0} privilege {1} on {2} {3} of {4}.", roleName, privilege, @object, objectName, dbName);

        Grpc.Status response = await _grpcClient.OperatePrivilegeAsync(new Grpc.OperatePrivilegeRequest()
        {
            Type = Grpc.OperatePrivilegeType.Grant,
            Entity = new Grpc.GrantEntity()
            {
                Role = new Grpc.RoleEntity()
                {
                    Name = roleName
                },
                Object = new Grpc.ObjectEntity()
                {
                    Name = @object
                },
                ObjectName = objectName,
                Grantor = new Grpc.GrantorEntity()
                {
                    Privilege = new Grpc.PrivilegeEntity()
                    {
                        Name = privilege
                    },
                },
                DbName = dbName
            }
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed grant role {0} privilege {1} on {2} {3} of {4}.", roleName, privilege, @object, objectName, dbName);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
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

        _log.LogDebug("Revoke role {0} privilege {1} on {2} {3}.", roleName, privilege, @object, objectName);

        Grpc.Status response = await _grpcClient.OperatePrivilegeAsync(new Grpc.OperatePrivilegeRequest()
        {
            Type = Grpc.OperatePrivilegeType.Revoke,
            Entity = new Grpc.GrantEntity()
            {
                Role = new Grpc.RoleEntity()
                {
                    Name = roleName
                },
                Object = new Grpc.ObjectEntity()
                {
                    Name = @object
                },
                ObjectName = objectName,
                Grantor = new Grpc.GrantorEntity()
                {
                    Privilege = new Grpc.PrivilegeEntity()
                    {
                        Name = privilege
                    },
                }
            }
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed Revoke role {0} privilege {1} on {2} {3}.", roleName, privilege, @object, objectName);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        _log.LogDebug("Select grant for role {0}.", roleName);

        Grpc.SelectGrantResponse response = await _grpcClient.SelectGrantAsync(new Grpc.SelectGrantRequest()
        {
            Entity = new Grpc.GrantEntity()
            {
                Role = new Grpc.RoleEntity()
                {
                    Name = roleName
                }
            }
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed select grant for role {0}.", roleName);
            throw new MilvusException(response.Status);
        }

        return MilvusGrantEntity.Parse(response.Entities);
    }

    ///<inheritdoc/>
    public async Task<IEnumerable<MilvusGrantEntity>> SelectGrantForRoleAndObjectAsync(
        string roleName,
        string @object,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(roleName);
        Verify.NotNullOrWhiteSpace(@object);
        Verify.NotNullOrWhiteSpace(objectName);
        _log.LogDebug("Select grant for role and object: {0}.", roleName);

        Grpc.SelectGrantResponse response = await _grpcClient.SelectGrantAsync(new Grpc.SelectGrantRequest()
        {
            Entity = new Grpc.GrantEntity()
            {
                Role = new Grpc.RoleEntity()
                {
                    Name = roleName
                },
                Object = new Grpc.ObjectEntity()
                {
                    Name = @object
                },
                ObjectName = objectName
            }
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Select grant for role and object: {0}.", roleName);
            throw new MilvusException(response.Status);
        }

        return MilvusGrantEntity.Parse(response.Entities);
    }
}
