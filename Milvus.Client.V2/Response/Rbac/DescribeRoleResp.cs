namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// The description of a role, including its users and privilege grants. Mirrors the Java
/// <c>DescribeRoleResp</c> / C++ <c>RoleDesc</c>.
/// </summary>
public sealed class DescribeRoleResp
{
    internal DescribeRoleResp(string roleName, IReadOnlyList<string> users, string description, IReadOnlyList<GrantItem> grants)
    {
        RoleName = roleName;
        Users = users;
        Description = description;
        Grants = grants;
    }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// The users assigned to the role.
    /// </summary>
    public IReadOnlyList<string> Users { get; }

    /// <summary>
    /// The role description, as set via <c>AlterRoleAsync</c>.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The privilege grants of the role.
    /// </summary>
    public IReadOnlyList<GrantItem> Grants { get; }

    internal static DescribeRoleResp FromGrpc(
        Grpc.SelectRoleResponse roleResponse, Grpc.SelectGrantResponse grantResponse)
    {
        string roleName = roleResponse.Results.Count > 0 ? roleResponse.Results[0].Role?.Name ?? "" : "";
        string description = roleResponse.Results.Count > 0 ? roleResponse.Results[0].Role?.Description ?? "" : "";
        IReadOnlyList<string> users = roleResponse.Results.Count > 0
            ? roleResponse.Results[0].Users.Select(u => u.Name).ToList()
            : Array.Empty<string>();
        IReadOnlyList<GrantItem> grants = grantResponse.Entities.Select(GrantItem.FromGrpc).ToList();
        return new DescribeRoleResp(roleName, users, description, grants);
    }
}
