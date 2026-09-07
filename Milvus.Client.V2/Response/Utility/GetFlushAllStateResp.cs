#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class GetFlushAllStateResp
{
    internal GetFlushAllStateResp(bool flushed) => Flushed = flushed;
    internal static GetFlushAllStateResp FromGrpc(Grpc.GetFlushAllStateResponse response) => new(response.Flushed);
    public bool Flushed { get; }
}
