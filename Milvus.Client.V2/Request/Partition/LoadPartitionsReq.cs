#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;
public sealed class LoadPartitionsReq
{
    public string CollectionName { get; set; } = "";
    public IReadOnlyList<string> PartitionNames { get; set; } = Array.Empty<string>();
    public int ReplicaNumber { get; set; } = 1;
    internal Grpc.LoadPartitionsRequest ToGrpcLoadPartitionsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(PartitionNames);
        var request = new Grpc.LoadPartitionsRequest { CollectionName = CollectionName, ReplicaNumber = ReplicaNumber };
        request.PartitionNames.AddRange(PartitionNames);
        return request;
    }
}
