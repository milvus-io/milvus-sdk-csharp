#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class ListUsersResp
{
    internal ListUsersResp(IReadOnlyList<string> users) => Users = users;
    internal static ListUsersResp FromGrpc(Grpc.ListCredUsersResponse response)
        => new(response.Usernames.ToList());
    public IReadOnlyList<string> Users { get; }
}
