namespace Milvus.Client.V2.Responses.Rls;

/// <summary>
/// The result of listing the principals that have stored tags in a collection.
/// </summary>
public sealed class ListRLSPrincipalsResp
{
    internal ListRLSPrincipalsResp(IReadOnlyList<string> principalNames) => PrincipalNames = principalNames;

    internal static ListRLSPrincipalsResp FromGrpc(Grpc.ListRLSPrincipalsResponse response)
        => new(response.PrincipalNames.ToList());

    /// <summary>
    /// The principal names with at least one stored tag in the collection.
    /// </summary>
    public IReadOnlyList<string> PrincipalNames { get; }
}
