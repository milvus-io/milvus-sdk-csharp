namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// The result of listing users.
/// </summary>
public sealed class ListUsersResp
{
    internal ListUsersResp(IReadOnlyList<string> users) => Users = users;
    internal static ListUsersResp FromGrpc(Grpc.ListCredUsersResponse response)
        => new(response.Usernames.ToList());

    /// <summary>
    /// The names of the users.
    /// </summary>
    public IReadOnlyList<string> Users { get; }
}
