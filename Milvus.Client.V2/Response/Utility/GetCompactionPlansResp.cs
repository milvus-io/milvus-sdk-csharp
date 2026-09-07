#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Types;
namespace Milvus.Client.V2.Responses.Utility;
public sealed class GetCompactionPlansResp
{
    internal GetCompactionPlansResp(CompactionState state) => State = state;
    internal static GetCompactionPlansResp FromGrpc(Grpc.GetCompactionPlansResponse response)
        => new((CompactionState)response.State);
    public CompactionState State { get; }
}
