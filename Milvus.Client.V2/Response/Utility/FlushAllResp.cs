#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class FlushAllResp
{
    internal FlushAllResp(ulong flushAllTimestamp) => FlushAllTimestamp = flushAllTimestamp;
#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
    internal static FlushAllResp FromGrpc(Grpc.FlushAllResponse response) => new(response.FlushAllTs);
#pragma warning restore CS0612
    public ulong FlushAllTimestamp { get; }
}
