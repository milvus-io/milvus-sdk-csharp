using Google.Protobuf.Collections;

namespace Milvus.Client;

/// <summary>
/// Information about a role, returned from <see cref="MilvusClient.SelectRoleAsync" /> and
/// <see cref="MilvusClient.SelectAllRolesAsync" />.
/// </summary>
/// <remarks>
/// For more details, see <see href="https://milvus.io/docs/rbac.md" />.
/// </remarks>
public sealed class MilvusRoleResult
{
    internal MilvusRoleResult(string role, RepeatedField<UserEntity> users)
    {
        Role = role;
        Users = users.Select(static u => u.Name).ToList();
    }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// The names of user in this role. Always empty if <c>includeUserInfo</c> was <c>false</c> in the call to
    /// <see cref="MilvusClient.SelectRoleAsync" /> or <see cref="MilvusClient.SelectAllRolesAsync" />.
    /// </summary>
    public IReadOnlyList<string> Users { get; }
}
