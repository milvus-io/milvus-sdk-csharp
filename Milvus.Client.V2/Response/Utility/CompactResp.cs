#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class CompactResp
{
    internal CompactResp(long compactionId) => CompactionId = compactionId;
    internal static CompactResp FromGrpc(Grpc.ManualCompactionResponse response) => new(response.CompactionID);
    public long CompactionId { get; }
}
