#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Responses.Partition;
public sealed class HasPartitionResp
{
    private HasPartitionResp(bool has) => Has = has;
    internal static HasPartitionResp FromGrpc(Grpc.BoolResponse response) => new(response.Value);
    public bool Has { get; }
}
