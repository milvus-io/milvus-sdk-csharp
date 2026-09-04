using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to add privileges to a privilege group.
/// </summary>
public sealed class AddPrivilegesToGroupReq
{
    /// <summary>
    /// The name of the privilege group.
    /// </summary>
    public string GroupName { get; set; } = "";

    /// <summary>
    /// The privileges to add to the group.
    /// </summary>
    public IReadOnlyList<string> Privileges { get; set; } = Array.Empty<string>();

    internal Grpc.OperatePrivilegeGroupRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(GroupName);
        Verify.NotNullOrEmpty(Privileges);

        var request = new Grpc.OperatePrivilegeGroupRequest
        {
            GroupName = GroupName,
            Type = Grpc.OperatePrivilegeGroupType.AddPrivilegesToGroup
        };
        foreach (string privilege in Privileges)
        {
            request.Privileges.Add(new Grpc.PrivilegeEntity { Name = privilege });
        }

        return request;
    }
}
