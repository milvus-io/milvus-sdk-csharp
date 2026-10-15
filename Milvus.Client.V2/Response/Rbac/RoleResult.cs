namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// A role and the users it is granted to.
/// </summary>
public sealed class RoleResult
{
    internal RoleResult(string role, IReadOnlyList<string> users)
    {
        Role = role;
        Users = users;
    }
    internal static RoleResult FromGrpc(Grpc.RoleResult result)
        => new(result.Role?.Name ?? "", result.Users.Select(u => u.Name).ToList());

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// The names of the users the role is granted to.
    /// </summary>
    public IReadOnlyList<string> Users { get; }
}
