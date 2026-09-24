using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to describe a user, returning the roles granted to the user.
/// </summary>
public sealed class DescribeUserReq
{
    /// <summary>
    /// The name of the user to describe.
    /// </summary>
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
