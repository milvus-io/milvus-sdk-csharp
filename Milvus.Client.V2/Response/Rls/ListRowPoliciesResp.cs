namespace Milvus.Client.V2.Responses.Rls;

/// <summary>
/// The result of listing the row policies of a collection.
/// </summary>
public sealed class ListRowPoliciesResp
{
    internal ListRowPoliciesResp(string dbName, string collectionName, IReadOnlyList<RowPolicyInfo> policies)
    {
        DbName = dbName;
        CollectionName = collectionName;
        Policies = policies;
    }

    internal static ListRowPoliciesResp FromGrpc(Grpc.ListRowPoliciesResponse response)
        => new(
            response.DbName,
            response.CollectionName,
            response.Policies.Select(RowPolicyInfo.FromGrpc).ToList());

    /// <summary>
    /// The database the row policies belong to.
    /// </summary>
    public string DbName { get; }

    /// <summary>
    /// The collection the row policies belong to.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The row policy definitions.
    /// </summary>
    public IReadOnlyList<RowPolicyInfo> Policies { get; }
}
