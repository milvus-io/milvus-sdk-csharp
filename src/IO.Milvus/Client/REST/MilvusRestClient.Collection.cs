using IO.Milvus.ApiSchema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    #region Collection
    ///<inheritdoc/>
    public async Task DropCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Delete collection {0}", collectionName);

        using HttpRequestMessage request = DropCollectionRequest
            .Create(collectionName)
            .Build();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Collection deletion failed: {0}, {1}", e.Message, responseContent);
            throw;
        }
    }

    ///<inheritdoc/>
    public Task<DescribeCollectionResponse> DescribeCollectionAsync(
        string collectionName,
        int collectionId,
        DateTime? dateTime = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        //var request = DescriptionCollectionRequest.Create(collectionName);
    }

    ///<inheritdoc/>
    public async Task CreateCollectionAsync(
        string collectionName, 
        ConsistencyLevel consistencyLevel, 
        IList<FieldType> fieldTypes,
        int shards_num = 1,
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
            this._log.LogError(e, "Failded check if a {0} exists: {0}, {1}", collectionName, e.Message, responseContent);
            throw;
        }

        var hasCollectionResponse = JsonSerializer.Deserialize<HasCollectionResponse> (responseContent);

        return hasCollectionResponse.Value;
    }

    ///<inheritdoc/>
    public Task ReleaseCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task LoadCollectionAsync(
        string collectionName, 
        int replicNumber = 1, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task GetCollectionStatisticsAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task ShowCollectionsAsync(
        IList<string> collectionNames = null, 
        int? type = null, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    #endregion
}
