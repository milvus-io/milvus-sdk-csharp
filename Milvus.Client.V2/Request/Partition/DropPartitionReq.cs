#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;
public sealed class DropPartitionReq
{
    public string CollectionName { get; set; } = "";
    public string PartitionName { get; set; } = "";
    internal Grpc.DropPartitionRequest ToGrpcDropPartitionRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PartitionName);
        return new Grpc.DropPartitionRequest { CollectionName = CollectionName, PartitionName = PartitionName };
    }
}
