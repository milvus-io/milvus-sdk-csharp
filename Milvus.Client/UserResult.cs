using Google.Protobuf.Collections;

namespace Milvus.Client;

/// <summary>
/// Information about a user, returned from <see cref="MilvusClient.SelectUserAsync" /> and
/// <see cref="MilvusClient.SelectAllUsersAsync" />.
/// </summary>
/// <remarks>
/// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
/// </remarks>
public sealed class UserResult
{
    internal UserResult(string user, RepeatedField<RoleEntity> roles)
    {
        User = user;
        Roles = roles.Select(static r => r.Name).ToList();
    }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string User { get; }

    /// <summary>
    /// The roles this user has. Always empty if <c>includeRoleInfo</c> was <c>false</c> in the call to
    /// <see cref="MilvusClient.SelectUserAsync" /> or <see cref="MilvusClient.SelectAllUsersAsync" />.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }
}
