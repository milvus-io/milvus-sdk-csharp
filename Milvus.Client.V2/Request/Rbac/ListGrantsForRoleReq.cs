#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class ListGrantsForRoleReq
{
    public string RoleName { get; set; } = "";
    internal Grpc.SelectGrantRequest ToGrpcSelectGrantRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.SelectGrantRequest
        {
            Entity = new Grpc.GrantEntity { Role = new Grpc.RoleEntity { Name = RoleName } }
        };
    }
}
