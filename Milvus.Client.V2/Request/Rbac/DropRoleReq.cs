#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class DropRoleReq
{
    public string RoleName { get; set; } = "";
    internal Grpc.DropRoleRequest ToGrpcDropRoleRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        return new Grpc.DropRoleRequest { RoleName = RoleName };
    }
}
