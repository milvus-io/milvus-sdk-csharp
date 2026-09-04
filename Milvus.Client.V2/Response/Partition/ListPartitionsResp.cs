#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Responses.Partition;
public sealed class ListPartitionsResp
{
    private ListPartitionsResp(IReadOnlyList<string> partitionNames, IReadOnlyList<long> partitionIds)
    {
        PartitionNames = partitionNames;
        PartitionIds = partitionIds;
    }
    internal static ListPartitionsResp FromGrpc(Grpc.ShowPartitionsResponse response)
        => new(response.PartitionNames.ToList(), response.PartitionIDs.ToList());
    public IReadOnlyList<string> PartitionNames { get; }
    public IReadOnlyList<long> PartitionIds { get; }
}
