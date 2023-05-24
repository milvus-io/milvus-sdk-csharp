using IO.Milvus.ApiSchema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Linq;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task CreateIndexAsync(
        string collectionName,
        string fieldName,
        MilvusIndexType indexType,
        MilvusMetricType milvusMetricType,
        IDictionary<string, string> extraParams,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create index {0}", collectionName);

        using HttpRequestMessage request = CreateIndexRequest
            .Create(collectionName, fieldName)
            .WithExtraParams(extraParams)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Create index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task DropIndexAsync(
        string collectionName,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Drop index {0}", collectionName);

        using HttpRequestMessage request = DropIndexRequest
            .Create(collectionName, fieldName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Drop index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Describe index {0}", collectionName);

        using HttpRequestMessage request = DescribeIndexRequest
            .Create(collectionName, fieldName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Drop index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<DescribeIndexResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed describe index: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.ToMilvusIndexes().ToList();
    }

    ///<inheritdoc/>
    public async Task<IndexBuildProgress> GetIndexBuildProgress(
        string collectionName,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get index build progress {0}, {1}", collectionName, fieldName);

        using HttpRequestMessage request = GetIndexBuildProgressRequest
            .Create(collectionName, fieldName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Failed get index build progress {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetIndexBuildProgressResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed get index build progress {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.ToIndexBuildProgress();
    }

    ///<inheritdoc/>
    public async Task<IndexState> GetIndexState(
        string collectionName,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get index state {0}, {1}", collectionName, fieldName);

        using HttpRequestMessage request = GetIndexStateRequest
            .Create(collectionName, fieldName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Failed get index state {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetIndexStateResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed get index build progress {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.IndexState;
    }
}
