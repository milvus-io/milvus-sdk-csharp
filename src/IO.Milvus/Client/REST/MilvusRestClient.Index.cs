using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = CreateIndexRequest
            .Create(collectionName, fieldName, milvusIndexType, milvusMetricType, dbName)
            .WithExtraParams(extraParams)
            .WithIndexName(indexName)
            .BuildRest();

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(indexName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/index", 
            new DropIndexRequest { CollectionName = collectionName, FieldName = fieldName, IndexName = indexName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusIndex>> DescribeIndexAsync(
        string collectionName,
        string fieldName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/index", 
            new DescribeIndexRequest { CollectionName = collectionName, FieldName = fieldName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var data = JsonSerializer.Deserialize<DescribeIndexResponse>(responseContent);
        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed describe index: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new MilvusException(data.Status);
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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/index/progress", 
            new GetIndexBuildProgressRequest { CollectionName = collectionName, FieldName = fieldName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var data = JsonSerializer.Deserialize<GetIndexBuildProgressResponse>(responseContent);
        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get index build progress {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new MilvusException(data.Status);
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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/state", 
            new GetIndexStateRequest { CollectionName = collectionName, FieldName = fieldName, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var data = JsonSerializer.Deserialize<GetIndexStateResponse>(responseContent);
        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get index build progress {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new MilvusException(data.Status);
        }

        return data.IndexState;
    }
}
