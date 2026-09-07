#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class CreateRoleReq
{
    public string RoleName { get; set; } = "";
    internal Grpc.CreateRoleRequest ToGrpcCreateRoleRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.CreateRoleRequest { Entity = new Grpc.RoleEntity { Name = RoleName } };
    }
}
