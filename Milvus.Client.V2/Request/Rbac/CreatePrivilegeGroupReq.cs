using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to create a privilege group.
/// </summary>
public sealed class CreatePrivilegeGroupReq
{
    /// <summary>
    /// The name of the privilege group to create.
    /// </summary>
    public string GroupName { get; set; } = "";
    internal Grpc.CreatePrivilegeGroupRequest ToGrpcCreatePrivilegeGroupRequest()
    {
        Verify.NotNullOrWhiteSpace(GroupName);
        return new Grpc.CreatePrivilegeGroupRequest { GroupName = GroupName };
    }
}
