#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Aliases;
public sealed class AlterAliasReq
{
    public string CollectionName { get; set; } = "";
    public string Alias { get; set; } = "";
    internal Grpc.AlterAliasRequest ToGrpcAlterAliasRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(Alias);
        return new Grpc.AlterAliasRequest { CollectionName = CollectionName, Alias = Alias };
    }
}
