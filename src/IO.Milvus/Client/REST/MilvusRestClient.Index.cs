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
        string indexName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        IDictionary<string, string> extraParams = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Create index {0}", collectionName);

        using HttpRequestMessage request = CreateIndexRequest
            .Create(collectionName, fieldName, milvusIndexType, milvusMetricType, dbName)
            .WithExtraParams(extraParams)
            .WithIndexName(indexName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Create index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task DropIndexAsync(
        string collectionName,
        string fieldName,
        string indexName = Constants.DEFAULT_INDEX_NAME,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Drop index {0}", collectionName);

        using HttpRequestMessage request = DropIndexRequest
            .Create(collectionName, fieldName, indexName, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Drop index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Describe index {0}", collectionName);

        using HttpRequestMessage request = DescribeIndexRequest
            .Create(collectionName, fieldName, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Describe index failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<DescribeIndexResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed describe index: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.ToMilvusIndexes().ToList();
    }

    ///<inheritdoc/>
    public async Task<IndexBuildProgress> GetIndexBuildProgressAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Get index build progress {0}, {1}", collectionName, fieldName);

        using HttpRequestMessage request = GetIndexBuildProgressRequest
            .Create(collectionName, fieldName, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Failed get index build progress {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetIndexBuildProgressResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get index build progress {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.ToIndexBuildProgress();
    }

    ///<inheritdoc/>
    public async Task<IndexState> GetIndexStateAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Get index state {0}, {1}", collectionName, fieldName);

        using HttpRequestMessage request = GetIndexStateRequest
            .Create(collectionName, fieldName, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Failed get index state {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetIndexStateResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get index build progress {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.IndexState;
    }
}
