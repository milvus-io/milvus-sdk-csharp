#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class CreatePrivilegeGroupReq
{
    public string GroupName { get; set; } = "";
    internal Grpc.CreatePrivilegeGroupRequest ToGrpcCreatePrivilegeGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(GroupName);
        return new Grpc.CreatePrivilegeGroupRequest { GroupName = GroupName };
    }
}
