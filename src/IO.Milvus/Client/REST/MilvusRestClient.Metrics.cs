using IO.Milvus.ApiSchema;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using IO.Milvus.Diagnostics;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task<MilvusMetrics> GetMetricsAsync(string request, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get metrics {0}", request);

        using HttpRequestMessage getMetricsRequest = GetMetricsRequest
            .Create(request)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(getMetricsRequest, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Get metrics failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetMetricsResponse>(responseContent);

        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed get metrics: {0}", data.Status.ErrorCode);
            throw new MilvusException(data.Status);
        }

        return new MilvusMetrics(data.Response, data.ComponentName);
    }
}