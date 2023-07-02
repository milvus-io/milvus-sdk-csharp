using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task<long> ManualCompactionAsync(
        long collectionId,
        DateTime? timeTravel = null,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Manual compaction {1}", collectionId);

        using HttpRequestMessage request = ManualCompactionRequest
            .Create(collectionId)
            .WithTimetravel(timeTravel)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError("Manual compaction failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<ManualCompactionResponse>(responseContent);

        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            throw new MilvusException(data.Status);
        }

        return data.CompactionId;
    }

    ///<inheritdoc/>
    public async Task<MilvusCompactionState> GetCompactionStateAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Get compaction state: {1}", compactionId);

        using HttpRequestMessage request = GetCompactionStateRequest
            .Create(compactionId)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError("Failed get compaction state: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetCompactionStateResponse>(responseContent);

        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get compaction state: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new MilvusException(data.Status);
        }

        return data.State;
    }

    ///<inheritdoc/>
    public async Task<MilvusCompactionPlans> GetCompactionPlansAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Get compaction plans: {1}", compactionId);

        using HttpRequestMessage request = GetCompactionPlansRequest
            .Create(compactionId)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError("Failed get compaction plans: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetCompactionPlansResponse>(responseContent);

        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get compaction plans: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new MilvusException(data.Status);
        }

        return MilvusCompactionPlans.From(data);
    }
}
