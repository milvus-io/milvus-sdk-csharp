using IO.Milvus.ApiSchema;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using IO.Milvus.Diagnostics;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task<MilvusMetrics> GetMetricsAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get metrics {0}", request);

        Grpc.GetMetricsRequest getMetricsRequest = GetMetricsRequest
            .Create(request)
            .BuildGrpc();

        Grpc.GetMetricsResponse response = await _grpcClient.GetMetricsAsync(getMetricsRequest, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get metrics failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return new MilvusMetrics(response.Response, response.ComponentName);
    }
}
