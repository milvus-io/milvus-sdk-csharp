#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;
public sealed class GetMetricsReq
{
    public string Request { get; set; } = "";
    internal Grpc.GetMetricsRequest ToGrpcGetMetricsRequest()
    {
        Verify.NotNullOrWhiteSpace(Request);
        return new Grpc.GetMetricsRequest { Request = Request };
    }
}
