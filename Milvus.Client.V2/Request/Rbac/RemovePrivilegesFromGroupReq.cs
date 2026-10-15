using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to remove privileges from a privilege group.
/// </summary>
public sealed class RemovePrivilegesFromGroupReq
{
    /// <summary>
    /// The name of the privilege group.
    /// </summary>
    public string GroupName { get; set; } = "";

    /// <summary>
    /// The privileges to remove from the group.
    /// </summary>
    public IReadOnlyList<string> Privileges { get; set; } = Array.Empty<string>();

    internal Grpc.OperatePrivilegeGroupRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(GroupName);
        Verify.NotNullOrEmpty(Privileges);

        var request = new Grpc.OperatePrivilegeGroupRequest
        {
            GroupName = GroupName,
            Type = Grpc.OperatePrivilegeGroupType.RemovePrivilegesFromGroup
        };
        foreach (string privilege in Privileges)
        {
            request.Privileges.Add(new Grpc.PrivilegeEntity { Name = privilege });
        }

        return request;
    }
}
