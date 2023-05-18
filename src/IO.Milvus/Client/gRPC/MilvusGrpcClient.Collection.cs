using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using IO.Milvus.ApiSchema;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using IO.Milvus.Diagnostics;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    #region Collection
    ///<inheritdoc/>
    public Task CreateCollectionAsync(
        string collectionName,
        ConsistencyLevel consistencyLevel, 
        IList<FieldType> fieldTypes, 
        int shards_num = 1,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task<DescribeCollectionResponse> DescribeCollectionAsync(
        string collectionName, 
        int collectionId, 
        DateTime? dateTime = null, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task DropCollectionAsync(
        string collectionName, 
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
    public async Task<bool> HasCollectionAsync(
        string collectionName, 
        DateTime? dateTime = null,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Check if a {0} exists", collectionName);

        Grpc.HasCollectionRequest request = HasCollectionRequest
            .Create(collectionName)
            .WithTimestamp(dateTime)
            .BuildGrpc();

        var response = await _grpcClient.HasCollectionAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if(response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            throw new MilvusException(response.Status.ErrorCode);
        }

        return response.Value;
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
    public Task ReleaseCollectionAsync(
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

