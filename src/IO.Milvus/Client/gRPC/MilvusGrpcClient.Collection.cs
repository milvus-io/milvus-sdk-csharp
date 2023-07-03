using Google.Protobuf;
using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    #region Collection
    ///<inheritdoc/>
    public async Task CreateCollectionAsync(
        string collectionName,
        IList<FieldType> fieldTypes,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        bool enableDynamicField = false,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);
        CreateCollectionRequest.ValidateFieldTypes(fieldTypes);

        _log.LogDebug("Create collection {0}, {1}", collectionName, consistencyLevel);

        Grpc.Status response = await _grpcClient.CreateCollectionAsync(new Grpc.CreateCollectionRequest()
        {
            CollectionName = collectionName,
            ConsistencyLevel = (Grpc.ConsistencyLevel)((int)consistencyLevel),
            ShardsNum = shardsNum,
            Schema = new CollectionSchema() { Fields = fieldTypes, EnableDynamicField = enableDynamicField }.ConvertCollectionSchema().ToByteString()
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Create collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Describe collection {0}", collectionName);

        Grpc.DescribeCollectionResponse response = await _grpcClient.DescribeCollectionAsync(new Grpc.DescribeCollectionRequest()
        {
            CollectionName = collectionName,
            DbName = dbName,
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Describe collection failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Drop collection {0}", collectionName);

        Grpc.Status response = await _grpcClient.DropCollectionAsync(new Grpc.DropCollectionRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Drop collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Get collection statistics {0}", collectionName);

        Grpc.GetCollectionStatisticsResponse response = await _grpcClient.GetCollectionStatisticsAsync(new Grpc.GetCollectionStatisticsRequest()
        {
            CollectionName = collectionName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get collection statistics: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Check if a {0} exists", collectionName);

        var response = await _grpcClient.HasCollectionAsync(new Grpc.HasCollectionRequest()
        {
            CollectionName = collectionName,
            TimeStamp = (ulong)(dateTime is not null ? dateTime.Value.ToUtcTimestamp() : 0),
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.GreaterThanOrEqualTo(replicaNumber, 1);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Load collection {0}", collectionName);

        Grpc.Status response = await _grpcClient.LoadCollectionAsync(new Grpc.LoadCollectionRequest()
        {
            CollectionName = collectionName,
            ReplicaNumber = replicaNumber,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Load collection failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task ReleaseCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Release collection {0}", collectionName);

        Grpc.Status response = await _grpcClient.ReleaseCollectionAsync(new Grpc.ReleaseCollectionRequest()
        {
            CollectionName = collectionName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Release collection failed: {0}, {1}", response.ErrorCode, response.Reason);
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
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Show collections {0}", collectionNames?.ToString());

        var request = new Grpc.ShowCollectionsRequest
        {
            Type = (Grpc.ShowType)showType,
            DbName = dbName
        };
        if (collectionNames is not null)
        {
            request.CollectionNames.AddRange(collectionNames);
        }

        Grpc.ShowCollectionsResponse response = await _grpcClient.ShowCollectionsAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Show collections failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return ToCollections(response).ToList();
    }

    ///<inheritdoc/>
    public async Task<long> GetLoadingProgressAsync(
       string collectionName,
       IList<string> partitionNames,
       CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        _log.LogDebug("Get loading progress for collection: {0}", collectionName);

        Grpc.GetLoadingProgressRequest request = new Grpc.GetLoadingProgressRequest()
        {
            CollectionName = collectionName,
        };
        if (partitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        Grpc.GetLoadingProgressResponse response = await _grpcClient.GetLoadingProgressAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get loading progress failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Progress;
    }

    ///<inheritdoc/>
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Get partition statistics: {0}", collectionName);

        Grpc.GetPartitionStatisticsResponse response = await _grpcClient.GetPartitionStatisticsAsync(
            new Grpc.GetPartitionStatisticsRequest()
            {
                CollectionName = collectionName,
                PartitionName = partitionName,
                DbName = dbName
            }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Get partition statistics failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Stats.ToDictionary(_ => _.Key, _ => _.Value);
    }
    #endregion

    #region Private =================================================================================================
    private static IEnumerable<MilvusCollection> ToCollections(Grpc.ShowCollectionsResponse response)
    {
        if (response.CollectionIds == null)
            yield break;

        for (int i = 0; i < response.CollectionIds.Count; i++)
        {
            yield return new MilvusCollection(
                response.CollectionIds[i],
                response.CollectionNames[i],
                TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamps[i]),
                response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1);
        }
    }
    #endregion
}