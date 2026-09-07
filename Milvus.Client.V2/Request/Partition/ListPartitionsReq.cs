#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;
public sealed class ListPartitionsReq
{
    public string CollectionName { get; set; } = "";
    internal Grpc.ShowPartitionsRequest ToGrpcShowPartitionsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.ShowPartitionsRequest { CollectionName = CollectionName };
    }
}
