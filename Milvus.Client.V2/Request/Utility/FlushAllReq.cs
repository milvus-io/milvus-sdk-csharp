#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.Utility;
public sealed class FlushAllReq
{
    internal static Grpc.FlushAllRequest ToGrpcFlushAllRequest() => new();
}
