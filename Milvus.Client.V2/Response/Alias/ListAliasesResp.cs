#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Responses.Aliases;
public sealed class ListAliasesResp
{
    private ListAliasesResp(IReadOnlyList<string> aliases)
    {
        Aliases = aliases;
    }
    internal static ListAliasesResp FromGrpc(Grpc.ListAliasesResponse response)
        => new(response.Aliases.ToList());
    public IReadOnlyList<string> Aliases { get; }
}
