using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(request);

        GetMetricsResponse response = await InvokeAsync(_grpcClient.GetMetricsAsync, new GetMetricsRequest
        {
            Request = request,
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return new MilvusMetrics(response.Response, response.ComponentName);
    }
}
