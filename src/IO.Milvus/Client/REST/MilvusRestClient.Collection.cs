using IO.Milvus.ApiSchema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    #region Collection
    ///<inheritdoc/>
    public async Task DropCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken)
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
            .Build();
            
        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync (request, cancellationToken);

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
    public Task<bool> HasCollectionAsync(
        string collectionName, 
        DateTime? dateTime, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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
