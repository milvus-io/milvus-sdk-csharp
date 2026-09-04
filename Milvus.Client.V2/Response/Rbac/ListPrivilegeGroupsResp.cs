namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// The result of listing privilege groups.
/// </summary>
public sealed class ListPrivilegeGroupsResp
{
    internal ListPrivilegeGroupsResp(IReadOnlyList<PrivilegeGroupInfo> groups) => Groups = groups;
    internal static ListPrivilegeGroupsResp FromGrpc(Grpc.ListPrivilegeGroupsResponse response)
        => new(response.PrivilegeGroups.Select(PrivilegeGroupInfo.FromGrpc).ToList());

    /// <summary>
    /// The privilege groups and the privileges they contain.
    /// </summary>
    public IReadOnlyList<PrivilegeGroupInfo> Groups { get; }

    /// <summary>
    /// The names of the privilege groups.
    /// </summary>
    public IReadOnlyList<string> GroupNames => Groups.Select(g => g.GroupName).ToList();
}

/// <summary>
/// A privilege group and the privileges it contains.
/// </summary>
public sealed class PrivilegeGroupInfo
{
    internal PrivilegeGroupInfo(string groupName, IReadOnlyList<string> privileges)
    {
        GroupName = groupName;
        Privileges = privileges;
    }

    internal static PrivilegeGroupInfo FromGrpc(Grpc.PrivilegeGroupInfo info)
        => new(info.GroupName, info.Privileges.Select(p => p.Name).ToList());

    /// <summary>
    /// The name of the privilege group.
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The privileges contained in the group.
    /// </summary>
    public IReadOnlyList<string> Privileges { get; }
}
