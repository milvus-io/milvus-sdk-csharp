namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// A user and the roles granted to it.
/// </summary>
public sealed class UserResult
{
    internal UserResult(string user, IReadOnlyList<string> roles, string description)
    {
        User = user;
        Roles = roles;
        Description = description;
    }
    internal static UserResult FromGrpc(Grpc.UserResult result)
        => new(result.User?.Name ?? "", result.Roles.Select(r => r.Name).ToList(), result.Description);

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string User { get; }

    /// <summary>
    /// The names of the roles granted to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// The user description, as set via <c>UpdateUserAsync</c>.
    /// </summary>
    public string Description { get; }
}
