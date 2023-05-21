using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using IO.Milvus.ApiSchema;
using Microsoft.Extensions.Logging;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using System.Linq;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    #region Collection
    ///<inheritdoc/>
    public async Task CreateCollectionAsync(
        string collectionName,
        IList<FieldType> fieldTypes, 
        ConsistencyLevel consistencyLevel = ConsistencyLevel.Session,
        int shards_num = 1,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create collection {0}, {1}", collectionName, consistencyLevel);

        Grpc.CreateCollectionRequest request = CreateCollectionRequest
            .Create(collectionName)
            .WithConsistencyLevel(consistencyLevel)
            .WithFieldTypes(fieldTypes)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateCollectionAsync(request);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Create collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Describe collection {0}", collectionName);

        Grpc.DescribeCollectionRequest request = DescribeCollectionRequest
            .Create(collectionName)
            .BuildGrpc();

        Grpc.DescribeCollectionResponse response = await _grpcClient.DescribeCollectionAsync(request);

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Describe collection failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return new DetailedMilvusCollection(
            response.Aliases,
            response.CollectionName,
            response.CollectionID,
            (ConsistencyLevel)response.ConsistencyLevel,
            TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamp),
            response.Schema.ToCollectioSchema(),
            response.ShardsNum,
            response.StartPositions.ToKeyDataPairs()
            );
    }

    ///<inheritdoc/>
    public async Task DropCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Drop collection {0}", collectionName);

        Grpc.DropCollectionRequest request = DropCollectionRequest
            .Create(collectionName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DropCollectionAsync(request);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Describe collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);   
        }
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get collection statistics {0}", collectionName);

        Grpc.GetCollectionStatisticsRequest request = GetCollectionStatisticsRequest
            .Create(collectionName)
            .BuildGrpc();

        Grpc.GetCollectionStatisticsResponse response = await _grpcClient.GetCollectionStatisticsAsync(request);

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get collection statistics: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Stats.ToDictionary(p => p.Key, p => p.Value);
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
            throw new MilvusException(response.Status);
        }

        return response.Value;
    }

    ///<inheritdoc/>
    public async Task LoadCollectionAsync(
        string collectionName, 
        int replicNumber = 1, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Load collection {0}", collectionName);

        Grpc.LoadCollectionRequest request = LoadCollectionRequest
            .Create(collectionName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.LoadCollectionAsync(request);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Load collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task ReleaseCollectionAsync(
        string collectionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Release collection {0}", collectionName);

        Grpc.ReleaseCollectionRequest request = ReleaseCollectionRequest
            .Create(collectionName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.ReleaseCollectionAsync(request);

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Release collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string> collectionNames = null,
        ShowType showType = ShowType.All, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Show collections {0}", collectionNames?.ToString());

        Grpc.ShowCollectionsRequest request = ShowCollectionsRequest
            .Create()
            .WithCollectionNames(collectionNames)
            .WithType(showType)
            .BuildGrpc();

        Grpc.ShowCollectionsResponse response = await _grpcClient.ShowCollectionsAsync(request);

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Show collections failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return ToCollections(response).ToList();
    }
    #endregion

    #region Private =================================================================================================
    private IEnumerable<MilvusCollection> ToCollections(Grpc.ShowCollectionsResponse response)
    {
        if (response.CollectionIds == null)
            yield break;

        for (int i = 0; i < response.CollectionIds.Count; i++)
        {
            yield return new MilvusCollection(
                response.CollectionIds[i],
                response.CollectionNames[i],
                TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamps[i]),
                response.InMemoryPercentages?.Any() == true ? response.InMemoryPercentages[i] : -1);
        }
    }
    #endregion
}