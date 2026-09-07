#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class DescribeUserReq
{
    public string UserName { get; set; } = "";
    internal Grpc.SelectUserRequest ToGrpcSelectUserRequest(bool includeRoleInfo = true)
    {
        Verify.NotNullOrWhiteSpace(UserName);
        return new Grpc.SelectUserRequest
        {
            User = new Grpc.UserEntity { Name = UserName },
            IncludeRoleInfo = includeRoleInfo
        };
    }
}
