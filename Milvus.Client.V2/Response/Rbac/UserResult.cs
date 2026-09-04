#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class UserResult
{
    internal UserResult(string user, IReadOnlyList<string> roles)
    {
        User = user;
        Roles = roles;
    }
    internal static UserResult FromGrpc(Grpc.UserResult result)
        => new(result.User.Name, result.Roles.Select(r => r.Name).ToList());
    public string User { get; }
    public IReadOnlyList<string> Roles { get; }
}
