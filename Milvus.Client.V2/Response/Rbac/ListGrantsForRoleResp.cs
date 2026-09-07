#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Rbac;
public sealed class ListGrantsForRoleResp
{
    internal ListGrantsForRoleResp(IReadOnlyList<GrantEntity> grants) => Grants = grants;
    internal static ListGrantsForRoleResp FromGrpc(Grpc.SelectGrantResponse response)
        => new(response.Entities.Select(GrantEntity.FromGrpc).ToList());
    public IReadOnlyList<GrantEntity> Grants { get; }
}
