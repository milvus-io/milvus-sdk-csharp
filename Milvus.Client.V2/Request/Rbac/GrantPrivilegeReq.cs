#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class GrantPrivilegeReq
{
    public string RoleName { get; set; } = "";
    public string Object { get; set; } = "";
    public string ObjectName { get; set; } = "";
    public string Privilege { get; set; } = "";
    internal Grpc.OperatePrivilegeRequest ToGrpcOperatePrivilegeRequest()
    {
        Verify.NotNullOrWhiteSpace(RoleName);
        Verify.NotNullOrWhiteSpace(Privilege);
        return new Grpc.OperatePrivilegeRequest
        {
            Entity = new Grpc.GrantEntity
            {
                Role = new Grpc.RoleEntity { Name = RoleName },
                Object = new Grpc.ObjectEntity { Name = Object },
                ObjectName = ObjectName,
                Grantor = new Grpc.GrantorEntity
                {
                    Privilege = new Grpc.PrivilegeEntity { Name = Privilege }
                }
            },
            Type = Grpc.OperatePrivilegeType.Grant
        };
    }
}
