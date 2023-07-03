using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task CreatePartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreatePartitionAsync, new CreatePartitionRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasPartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        BoolResponse response = await InvokeAsync(_grpcClient.HasPartitionAsync, new HasPartitionRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName,
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Value;
    }

    /// <inheritdoc />
    public async Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        ShowPartitionsResponse response = await InvokeAsync(_grpcClient.ShowPartitionsAsync, new ShowPartitionsRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        List<MilvusPartition> partitions = new List<MilvusPartition>();
        if (response.PartitionIDs is not null)
        {
            for (int i = 0; i < response.PartitionIDs.Count; i++)
            {
                partitions.Add(new MilvusPartition(
                    response.PartitionIDs[i],
                    response.PartitionNames[i],
                    TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamps[i]),
                    response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1));
            }
        }

        return partitions;
    }

    /// <inheritdoc />
    public async Task LoadPartitionsAsync(
        string collectionName,
        IList<string> partitionNames,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        LoadPartitionsRequest request = new LoadPartitionsRequest
        {
            CollectionName = collectionName,
            ReplicaNumber = replicaNumber,
            DbName = dbName
        };
        request.PartitionNames.AddRange(partitionNames);

        await InvokeAsync(_grpcClient.LoadPartitionsAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReleasePartitionAsync(
        string collectionName,
        IList<string> partitionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        ReleasePartitionsRequest request = new()
        {
            CollectionName = collectionName,
            DbName = dbName
        };
        request.PartitionNames.AddRange(partitionNames);

        await InvokeAsync(_grpcClient.ReleasePartitionsAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DropPartitionsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropPartitionAsync, new DropPartitionRequest()
        {
            CollectionName = collectionName,
            PartitionName = partitionName
        }, cancellationToken).ConfigureAwait(false);
    }
}
