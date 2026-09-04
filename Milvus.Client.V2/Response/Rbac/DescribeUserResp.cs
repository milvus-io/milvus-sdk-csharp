namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// The result of describing a user, including the roles granted to it.
/// </summary>
public sealed class DescribeUserResp
{
    internal DescribeUserResp(UserResult? user) => User = user;
    internal static DescribeUserResp FromGrpc(Grpc.SelectUserResponse response)
    {
        UserResult? user = response.Results.Count == 0 ? null : UserResult.FromGrpc(response.Results[0]);
        return new DescribeUserResp(user);
    }

    /// <summary>
    /// The described user, or <c>null</c> when the user does not exist.
    /// </summary>
    public UserResult? User { get; }
}
