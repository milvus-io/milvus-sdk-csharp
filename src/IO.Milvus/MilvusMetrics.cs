namespace IO.Milvus;

/// <summary>
/// Milvus metrics
/// </summary>
public class MilvusMetrics
{
    /// <summary>
    /// Constructor a milvus metrics.
    /// </summary>
    /// <param name="response">response is of jsonic format.</param>
    /// <param name="componentName">metrics from which component.</param>
    public MilvusMetrics(string response, string componentName)
    {
        Response = response;
        ComponentName = componentName;
    }

    /// <summary>
    /// response is of jsonic format.
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// metrics from which component.
    /// </summary>
    public string ComponentName { get; }
}