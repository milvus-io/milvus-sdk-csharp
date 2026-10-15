using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to drop a privilege group.
/// </summary>
public sealed class DropPrivilegeGroupReq
{
    /// <summary>
    /// The name of the privilege group to drop.
    /// </summary>
    public string GroupName { get; set; } = "";
    internal Grpc.DropPrivilegeGroupRequest ToGrpcDropPrivilegeGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(GroupName);
        return new Grpc.DropPrivilegeGroupRequest { GroupName = GroupName };
    }
}
