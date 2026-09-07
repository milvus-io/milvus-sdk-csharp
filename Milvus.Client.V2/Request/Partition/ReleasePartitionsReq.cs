#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Partition;
public sealed class ReleasePartitionsReq
{
    public string CollectionName { get; set; } = "";
    public IReadOnlyList<string> PartitionNames { get; set; } = Array.Empty<string>();
    internal Grpc.ReleasePartitionsRequest ToGrpcReleasePartitionsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(PartitionNames);
        var request = new Grpc.ReleasePartitionsRequest { CollectionName = CollectionName };
        request.PartitionNames.AddRange(PartitionNames);
        return request;
    }
}
