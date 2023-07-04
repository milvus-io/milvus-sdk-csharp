using Google.Protobuf;
using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
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
        ApiSchema.CreateCollectionRequest.ValidateFieldTypes(fieldTypes);

        await InvokeAsync(_grpcClient.CreateCollectionAsync, new CreateCollectionRequest
        {
            CollectionName = collectionName,
            ConsistencyLevel = (ConsistencyLevel)(int)consistencyLevel,
            ShardsNum = shardsNum,
            Schema = new CollectionSchema() { Name = collectionName, Fields = fieldTypes, EnableDynamicField = enableDynamicField }.ConvertCollectionSchema().ToByteString()
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        DescribeCollectionResponse response = await InvokeAsync(_grpcClient.DescribeCollectionAsync, new DescribeCollectionRequest
        {
            CollectionName = collectionName,
            DbName = dbName,
        }, r=> r.Status, cancellationToken).ConfigureAwait(false);

        return new DetailedMilvusCollection(
            response.Aliases,
            response.CollectionName,
            response.CollectionID,
            (MilvusConsistencyLevel)response.ConsistencyLevel,
            TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamp),
            response.Schema.ToCollectionSchema(),
            response.ShardsNum,
            response.StartPositions.ToKeyDataPairs());
    }

    ///<inheritdoc/>
    public async Task RenameCollectionAsync(
        string oldName,
        string newName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(oldName);
        Verify.NotNullOrWhiteSpace(newName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.RenameCollectionAsync, new RenameCollectionRequest
        {
            OldName = oldName,
            NewName = newName,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DropCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropCollectionAsync, new DropCollectionRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        GetCollectionStatisticsResponse response = await InvokeAsync(_grpcClient.GetCollectionStatisticsAsync, new GetCollectionStatisticsRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Stats.ToDictionary();
    }

    /// <inheritdoc />
    public async Task<bool> HasCollectionAsync(
        string collectionName,
        DateTime? dateTime = null,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        BoolResponse response = await InvokeAsync(_grpcClient.HasCollectionAsync, new HasCollectionRequest
        {
            CollectionName = collectionName,
            TimeStamp = (ulong)(dateTime is not null ? dateTime.Value.ToUtcTimestamp() : 0),
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Value;
    }

    /// <inheritdoc />
    public async Task LoadCollectionAsync(
        string collectionName,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.GreaterThanOrEqualTo(replicaNumber, 1);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.LoadCollectionAsync, new LoadCollectionRequest
        {
            CollectionName = collectionName,
            ReplicaNumber = replicaNumber,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReleaseCollectionAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.ReleaseCollectionAsync, new ReleaseCollectionRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string> collectionNames = null,
        ShowType showType = ShowType.All,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        ShowCollectionsRequest request = new ShowCollectionsRequest
        {
            Type = (Grpc.ShowType)showType,
            DbName = dbName
        };
        if (collectionNames is not null)
        {
            request.CollectionNames.AddRange(collectionNames);
        }

        ShowCollectionsResponse response = await InvokeAsync(_grpcClient.ShowCollectionsAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        List<MilvusCollection> collections = new List<MilvusCollection>();
        if (response.CollectionIds is not null)
        {
            for (int i = 0; i < response.CollectionIds.Count; i++)
            {
                collections.Add(new MilvusCollection(
                    response.CollectionIds[i],
                    response.CollectionNames[i],
                    TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamps[i]),
                    response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1));
            }
        }

        return collections;
    }

    /// <inheritdoc />
    public async Task<long> GetLoadingProgressAsync(
       string collectionName,
       IList<string> partitionNames,
       CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        GetLoadingProgressRequest request = new()
        {
            CollectionName = collectionName,
        };
        if (partitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        GetLoadingProgressResponse response = await InvokeAsync(_grpcClient.GetLoadingProgressAsync, request, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Progress;
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        GetPartitionStatisticsResponse response = await InvokeAsync(_grpcClient.GetPartitionStatisticsAsync, new GetPartitionStatisticsRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Stats.ToDictionary();
    }
}
