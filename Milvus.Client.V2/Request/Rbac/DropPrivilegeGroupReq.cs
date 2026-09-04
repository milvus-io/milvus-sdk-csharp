#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class DropPrivilegeGroupReq
{
    public string GroupName { get; set; } = "";
    internal Grpc.DropPrivilegeGroupRequest ToGrpcDropPrivilegeGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(GroupName);
        return new Grpc.DropPrivilegeGroupRequest { GroupName = GroupName };
    }
}
