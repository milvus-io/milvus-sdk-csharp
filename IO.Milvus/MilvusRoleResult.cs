using IO.Milvus.Grpc;

namespace IO.Milvus;

/// <summary>
/// Milvus role result.
/// </summary>
/// <remarks>
/// <see href="https://milvus.io/docs/rbac.md"/>
/// </remarks>
public sealed class MilvusRoleResult
{
    internal MilvusRoleResult(string roleName, IEnumerable<string> users)
    {
        RoleName = roleName;
        Users = users;
    }

    /// <summary>
    /// Role name.
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// Users that have the role.
    /// </summary>
    public IEnumerable<string> Users { get; }

    internal static IEnumerable<MilvusRoleResult> Parse(IEnumerable<RoleResult> results)
    {
        if (results == null)
            yield break;

        foreach (RoleResult result in results)
        {
            yield return new MilvusRoleResult(
                result.Role.Name,
                result.Users?.Select(static u => u.Name) ?? Enumerable.Empty<string>());
        }
    }
}
