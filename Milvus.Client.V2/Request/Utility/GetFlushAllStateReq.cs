#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.Utility;
public sealed class GetFlushAllStateReq
{
    public ulong FlushAllTimestamp { get; set; }
#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
    internal Grpc.GetFlushAllStateRequest ToGrpcGetFlushAllStateRequest()
        => new() { FlushAllTs = FlushAllTimestamp };
#pragma warning restore CS0612
}
