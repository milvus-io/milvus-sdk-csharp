using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task<MilvusMetrics> GetMetricsAsync(string request, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(request);

        using HttpRequestMessage getMetricsRequest = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/metrics",
            new GetMetricsRequest { Request = request });

        string responseContent = await ExecuteHttpRequestAsync(getMetricsRequest, cancellationToken).ConfigureAwait(false);

        var data = JsonSerializer.Deserialize<GetMetricsResponse>(responseContent);
        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get metrics: {0}", data.Status.ErrorCode);
            throw new MilvusException(data.Status);
        }

        return new MilvusMetrics(data.Response, data.ComponentName);
    }
}