using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(request);

        _log.LogDebug("Get metrics {0}", request);

        Grpc.GetMetricsResponse response = await _grpcClient.GetMetricsAsync(new Grpc.GetMetricsRequest()
        {
            Request = request,
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get metrics failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return new MilvusMetrics(response.Response, response.ComponentName);
    }
}
