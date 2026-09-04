namespace Milvus.Client.V2.Responses.Rbac;

/// <summary>
/// The result of listing the privilege grants of a role.
/// </summary>
public sealed class ListGrantsForRoleResp
{
    internal ListGrantsForRoleResp(IReadOnlyList<GrantEntity> grants) => Grants = grants;
    internal static ListGrantsForRoleResp FromGrpc(Grpc.SelectGrantResponse response)
        => new(response.Entities.Select(GrantEntity.FromGrpc).ToList());

    /// <summary>
    /// The privilege grants of the role.
    /// </summary>
    public IReadOnlyList<GrantEntity> Grants { get; }
}
