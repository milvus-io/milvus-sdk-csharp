#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class ListRolesReq
{
    internal static Grpc.SelectRoleRequest ToGrpcSelectRoleRequest()
        => new() { Role = new Grpc.RoleEntity { Name = "" } };
}
