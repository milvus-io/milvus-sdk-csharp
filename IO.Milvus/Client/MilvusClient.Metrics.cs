using IO.Milvus.Grpc;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Get metrics.
    /// </summary>
    /// <param name="request">Request in JSON format.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>metrics from which component.</returns>
    public async Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(request);

        GetMetricsResponse response = await InvokeAsync(_grpcClient.GetMetricsAsync, new GetMetricsRequest
        {
            Request = request
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return new MilvusMetrics(response.Response, response.ComponentName);
    }
}
