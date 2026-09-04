#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Aliases;
public sealed class DescribeAliasReq
{
    public string Alias { get; set; } = "";
    internal Grpc.DescribeAliasRequest ToGrpcDescribeAliasRequest()
    {
        Verify.NotNullOrWhiteSpace(Alias);
        return new Grpc.DescribeAliasRequest { Alias = Alias };
    }
}
