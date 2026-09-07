#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Utility;
public sealed class GetMetricsResp
{
    internal GetMetricsResp(string response, string componentName)
    {
        Response = response;
        ComponentName = componentName;
    }
    internal static GetMetricsResp FromGrpc(Grpc.GetMetricsResponse response)
        => new(response.Response, response.ComponentName);
    public string Response { get; }
    public string ComponentName { get; }
}
