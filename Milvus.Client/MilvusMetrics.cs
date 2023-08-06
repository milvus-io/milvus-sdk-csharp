namespace Milvus.Client;

/// <summary>
/// Contains the response to a request for metrics via <see cref="MilvusClient.GetMetricsAsync" />.
/// </summary>
public sealed class MilvusMetrics
{
    internal MilvusMetrics(string response, string componentName)
    {
        Response = response;
        ComponentName = componentName;
    }

    /// <summary>
    /// A response in JSON format.
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// The name of the component from which the metrics were returned.
    /// </summary>
    public string ComponentName { get; }
}
