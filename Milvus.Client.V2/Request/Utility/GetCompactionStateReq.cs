#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.Utility;
public sealed class GetCompactionStateReq
{
    public long CompactionId { get; set; }
    internal Grpc.GetCompactionStateRequest ToGrpcGetCompactionStateRequest()
        => new() { CompactionID = CompactionId };
}
