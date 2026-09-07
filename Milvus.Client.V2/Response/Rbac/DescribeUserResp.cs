#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class DescribeUserResp
{
    internal DescribeUserResp(UserResult? user) => User = user;
    internal static DescribeUserResp FromGrpc(Grpc.SelectUserResponse response)
    {
        UserResult? user = response.Results.Count == 0 ? null : UserResult.FromGrpc(response.Results[0]);
        return new DescribeUserResp(user);
    }
    public UserResult? User { get; }
}
