#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.Utility;
public sealed class GetCompactionPlansReq
{
    public long CompactionId { get; set; }
    internal Grpc.GetCompactionPlansRequest ToGrpcGetCompactionPlansRequest()
        => new() { CompactionID = CompactionId };
}
