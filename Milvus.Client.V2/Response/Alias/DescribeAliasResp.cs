#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Responses.Aliases;
public sealed class DescribeAliasResp
{
    private DescribeAliasResp(string collectionName, string alias)
    {
        CollectionName = collectionName;
        Alias = alias;
    }
    internal static DescribeAliasResp FromGrpc(Grpc.DescribeAliasResponse response)
        => new(response.Collection, response.Alias);
    public string CollectionName { get; }
    public string Alias { get; }
}
