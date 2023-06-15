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
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shards_num = 1,
        bool enableDynamicField = false,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create collection {0}, {1}", collectionName, consistencyLevel);

        Grpc.CreateCollectionRequest request = CreateCollectionRequest
            .Create(collectionName,dbName,enableDynamicField)
            .WithShardsNum(shards_num)
            .WithConsistencyLevel(consistencyLevel)
            .WithFieldTypes(fieldTypes)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreateCollectionAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Create collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName, 
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Describe collection {0}", collectionName);

        Grpc.DescribeCollectionRequest request = DescribeCollectionRequest
            .Create(collectionName, dbName)
            .BuildGrpc();

        Grpc.DescribeCollectionResponse response = await _grpcClient.DescribeCollectionAsync(request,_callOptions.WithCancellationToken(cancellationToken));
        
        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Describe collection failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return new DetailedMilvusCollection(
            response.Aliases,
            response.CollectionName,
            response.CollectionID,
            (MilvusConsistencyLevel)response.ConsistencyLevel,
            TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamp),
            response.Schema.ToCollectionSchema(),
            response.ShardsNum,
            response.StartPositions.ToKeyDataPairs()
            );
    }

    ///<inheritdoc/>
    public async Task DropCollectionAsync(
        string collectionName, 
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Drop collection {0}", collectionName);

        Grpc.DropCollectionRequest request = DropCollectionRequest
            .Create(collectionName,dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DropCollectionAsync(request,_callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Drop collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);   
        }
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get collection statistics {0}", collectionName);

        Grpc.GetCollectionStatisticsRequest request = GetCollectionStatisticsRequest
            .Create(collectionName, dbName)
            .BuildGrpc();

        Grpc.GetCollectionStatisticsResponse response = await _grpcClient.GetCollectionStatisticsAsync(request,_callOptions.WithCancellationToken(cancellationToken));

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
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Check if a {0} exists", collectionName);

        Grpc.HasCollectionRequest request = HasCollectionRequest
            .Create(collectionName, dbName)
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
        int replicaNumber = 1, 
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Load collection {0}", collectionName);

        Grpc.LoadCollectionRequest request = LoadCollectionRequest
            .Create(collectionName, dbName)
            .WithReplicaNumber(replicaNumber)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.LoadCollectionAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Load collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task ReleaseCollectionAsync(
        string collectionName, 
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Release collection {0}", collectionName);

        Grpc.ReleaseCollectionRequest request = ReleaseCollectionRequest
            .Create(collectionName,dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.ReleaseCollectionAsync(request, _callOptions.WithCancellationToken(cancellationToken));

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
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Show collections {0}", collectionNames?.ToString());

        Grpc.ShowCollectionsRequest request = ShowCollectionsRequest
            .Create(dbName)
            .WithCollectionNames(collectionNames)
            .WithType(showType)
            .BuildGrpc();

        Grpc.ShowCollectionsResponse response = await _grpcClient.ShowCollectionsAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Show collections failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return ToCollections(response).ToList();
    }

    ///<inheritdoc/>
    public async Task<long> GetLoadingProgressAsync(
       string collectionName,
       IList<string> partitionNames,
       CancellationToken cancellationToken)
    {
        this._log.LogDebug("Get loading progress for collection: {0}", collectionName);

        Grpc.GetLoadingProgressRequest request = GetLoadingProgressRequest
            .Create(collectionName)
            .WithPartitionNames(partitionNames)
            .BuildGrpc();
 
        Grpc.GetLoadingProgressResponse response = await _grpcClient.GetLoadingProgressAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get loading progress failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Progress;
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName, 
        string partitionName, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Get partition statistics: {0}", collectionName);

        Grpc.GetPartitionStatisticsRequest request = GetPartitionStatisticsRequest
            .Create(collectionName,partitionName)
            .BuildGrpc();

        Grpc.GetPartitionStatisticsResponse response = await _grpcClient.GetPartitionStatisticsAsync(
            request, 
            _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Get partition statistics failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Stats.ToDictionary(_ => _.Key, _ => _.Value);
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