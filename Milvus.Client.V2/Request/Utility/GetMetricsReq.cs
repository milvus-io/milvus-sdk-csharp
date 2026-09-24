using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the metrics of the Milvus server.
/// </summary>
public sealed class GetMetricsReq
{
    /// <summary>
    /// The metric request string, e.g. <c>{"metric_type":"system_info"}</c> (the server requires a JSON object
    /// carrying a <c>metric_type</c> or <c>req_type</c> key).
    /// </summary>
    public string Request { get; set; } = "";
    internal Grpc.GetMetricsRequest ToGrpcGetMetricsRequest()
    {
        Verify.NotNullOrWhiteSpace(Request);
        return new Grpc.GetMetricsRequest { Request = Request };
    }
}
