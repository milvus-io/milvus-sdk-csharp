using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
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
        Verify.GreaterThan(collectionId, 0);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/compaction",
            new ManualCompactionRequest { CollectionId = collectionId, TimeTravel = timeTravel is not null ? TimestampUtils.ToUtcTimestamp(timeTravel.Value) : 0 });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

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
        Verify.GreaterThan(compactionId, 0);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/compaction/state",
            new GetCompactionStateRequest { CompactionId = compactionId });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var data = JsonSerializer.Deserialize<GetCompactionStateResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.State;
    }

    ///<inheritdoc/>
    public async Task<MilvusCompactionPlans> GetCompactionPlansAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0); // TODO: The other's had this and this one didn't; was it intentional?

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/compaction/plans",
            new GetCompactionPlansRequest { CompactionId = compactionId });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var data = JsonSerializer.Deserialize<GetCompactionPlansResponse>(responseContent);
        ValidateStatus(data.Status);

        return MilvusCompactionPlans.From(data);
    }
}
