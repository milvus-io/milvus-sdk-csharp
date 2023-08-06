namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Gets metrics.
    /// </summary>
    /// <param name="request">Request in JSON format.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(request);

        GetMetricsResponse response = await InvokeAsync(GrpcClient.GetMetricsAsync, new GetMetricsRequest
        {
            Request = request
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return new MilvusMetrics(response.Response, response.ComponentName);
    }
}
