using IO.Milvus.ApiSchema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Linq;
using IO.Milvus.Diagnostics;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task DropCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Delete collection {0}", collectionName);

        using HttpRequestMessage request = DropCollectionRequest
            .Create(collectionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Delete collection failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Describe collection {0}", collectionName);

        using HttpRequestMessage request = DescribeCollectionRequest
            .Create(collectionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Describe collection failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<DescribeCollectionResponse>(responseContent);

        if (data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed Describe collections: {0}", data.Status.ErrorCode);
            throw new MilvusException(data.Status);
        }

        return data.ToDetailedMilvusCollection();
    }

    ///<inheritdoc/>
    public async Task CreateCollectionAsync(
        string collectionName, 
        IList<FieldType> fieldTypes,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session, 
        int shards_num = 1,
        bool enableDynamicField = false,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create collection {0}, {1}",collectionName, consistencyLevel);

        using HttpRequestMessage request = CreateCollectionRequest
            .Create(collectionName)
            .WithConsistencyLevel(consistencyLevel)
            .WithFieldTypes(fieldTypes)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Collection creation failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<bool> HasCollectionAsync(
        string collectionName, 
        DateTime? dateTime = null, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Check if a {0} exists", collectionName);

        using HttpRequestMessage request = HasCollectionRequest
            .Create(collectionName)
            .WithTimestamp(dateTime) 
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception e)
        {
            this._log.LogError(e, "Failed check if a {0} exists: {0}, {1}", collectionName, e.Message, responseContent);
            throw;
        }

        if (string.IsNullOrEmpty(responseContent) || responseContent == "{}")
            return false;

        var hasCollectionResponse = JsonSerializer.Deserialize<HasCollectionResponse> (responseContent);

        return hasCollectionResponse.Value;
    }

    ///<inheritdoc/>
    public async Task ReleaseCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Release collection: {0}", collectionName);

        using HttpRequestMessage request = ReleaseCollectionRequest
            .Create(collectionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception e)
        {
            this._log.LogError(e, "Failed release collection: {0}, {1}", collectionName, e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task LoadCollectionAsync(
        string collectionName, 
        int replicaNumber = 1, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Load collection: {0}", collectionName);

        using HttpRequestMessage request = LoadCollectionRequest
            .Create(collectionName)
            .WithReplicaNumber(replicaNumber)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception e)
        {
            this._log.LogError(e, "Failed load collection: {0}, {1}", collectionName, e.Message, responseContent);
            throw;
        }
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string,string>> GetCollectionStatisticsAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get collection statistics: {0}", collectionName);

        using HttpRequestMessage request = GetCollectionStatisticsRequest
            .Create(collectionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception e)
        {
            this._log.LogError(e, "Failed get collection statistics: {0}, {1}", collectionName, e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetCollectionStatisticsResponse>(responseContent);

        return data.Statistics;
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string> collectionNames = null, 
        ShowType showType = ShowType.All, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Show collections");

        using HttpRequestMessage request = ShowCollectionsRequest
            .Create()
            .WithCollectionNames(collectionNames)
            .WithType(showType)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception e)
        {
            this._log.LogError(e, "Failed show collections: {0}, {1}", collectionNames?.ToString(), e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<ShowCollectionsResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed show collections: {0}", data.Status.ErrorCode);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.ToCollections().ToList();
    }

    ///<inheritdoc/>
    public Task<long> GetLoadingProgressAsync(
        string collectionName, 
        IList<string> partitionNames = null, 
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"Not supported in milvus restful api");
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName, 
        string partitionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get partition statistics: {0}, {1}", collectionName, partitionName);

        using HttpRequestMessage request = GetPartitionStatisticsRequest
            .Create(collectionName,partitionName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception e)
        {
            this._log.LogError(e, "Get partition statistics: {0}, {1}", collectionName, e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<GetPartitionStatisticsResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get partition statistics: {0}", data.Status.ErrorCode);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.Stats;
    }
}
