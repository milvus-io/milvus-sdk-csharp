#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;
public sealed class GetCompactionStateResp
{
    internal GetCompactionStateResp(CompactionState state) => State = state;
    internal static GetCompactionStateResp FromGrpc(Grpc.GetCompactionStateResponse response)
        => new((CompactionState)response.State);
    public CompactionState State { get; }
}
