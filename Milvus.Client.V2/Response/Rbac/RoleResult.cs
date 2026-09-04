#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class RoleResult
{
    internal RoleResult(string role, IReadOnlyList<string> users)
    {
        Role = role;
        Users = users;
    }
    internal static RoleResult FromGrpc(Grpc.RoleResult result)
        => new(result.Role.Name, result.Users.Select(u => u.Name).ToList());
    public string Role { get; }
    public IReadOnlyList<string> Users { get; }
}
