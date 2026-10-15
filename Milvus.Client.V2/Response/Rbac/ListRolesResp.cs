namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// The result of listing roles.
/// </summary>
public sealed class ListRolesResp
{
    internal ListRolesResp(IReadOnlyList<RoleResult> roles) => Roles = roles;
    internal static ListRolesResp FromGrpc(Grpc.SelectRoleResponse response)
        => new(response.Results.Select(RoleResult.FromGrpc).ToList());

    /// <summary>
    /// The roles and their member users.
    /// </summary>
    public IReadOnlyList<RoleResult> Roles { get; }
}
