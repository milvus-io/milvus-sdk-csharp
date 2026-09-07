#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Aliases;
public sealed class ListAliasesReq
{
    public string? CollectionName { get; set; }
    internal Grpc.ListAliasesRequest ToGrpcListAliasesRequest()
        => new() { CollectionName = CollectionName ?? "" };
}
