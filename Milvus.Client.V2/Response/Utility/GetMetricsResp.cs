namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a metrics request.
/// </summary>
public sealed class GetMetricsResp
{
    internal GetMetricsResp(string response, string componentName)
    {
        Response = response;
        ComponentName = componentName;
    }
    internal static GetMetricsResp FromGrpc(Grpc.GetMetricsResponse response)
        => new(response.Response, response.ComponentName);

    /// <summary>
    /// The metrics payload, in the server's configured metrics format.
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// The name of the component that produced the metrics.
    /// </summary>
    public string ComponentName { get; }
}
