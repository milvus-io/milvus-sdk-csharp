using IO.Milvus.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus;

/// <summary>
/// Extension methods for <see cref="Client.IMilvusClient"/>
/// </summary>
public static class MilvusClientExtensions
{
    /// <summary>
    /// Use showCollection() to check loading percentages of the collection.
    /// </summary>
    /// <remarks>
    /// If the inMemory percentage is 100, that means the collection has finished loading.
    ///  Otherwise, this thread will sleep a small interval and check again.
    ///  If waiting time exceed timeout, exist the circle
    /// </remarks>
    /// <param name="milvusClient"><see cref="IMilvusClient"/></param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="waitingInterval">Waiting interval.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="TimeoutException"></exception>
    public static async Task WaitForLoadingCollection(
        this IMilvusClient milvusClient,
        string collectionName,
        IList<string> partitionName,
        TimeSpan waitingInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (partitionName?.Any() != true)
        {
            IList<MilvusCollection> collections = await milvusClient.ShowCollectionsAsync(new[] { collectionName },cancellationToken:cancellationToken);

            MilvusCollection collection = collections.FirstOrDefault(c => c.CollectionName == collectionName);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection name not found: {collectionName}");
            }

            while (collection.InMemoryPercentage < 100)
            {
                await Task.Delay(waitingInterval, cancellationToken);
                timeout -= waitingInterval;
                if (timeout <= TimeSpan.Zero)
                {
                    throw new TimeoutException($"Wait for loading collection {collectionName} timeout");
                }
                collections = await milvusClient.ShowCollectionsAsync(new[] { collectionName }, cancellationToken: cancellationToken);
            }
        }
        else
        {
            IList<MilvusPartition> partitions = await milvusClient.ShowPartitionsAsync(collectionName, cancellationToken: cancellationToken);

            MilvusPartition partition = partitions.FirstOrDefault(c => c.PartitionName == collectionName);
            if (partition == null)
            {
                throw new InvalidOperationException($"Collection name not found: {collectionName}");
            }

            while (partition.InMemoryPercentage < 100)
            {
                await Task.Delay(waitingInterval, cancellationToken);
                timeout -= waitingInterval;
                if (timeout <= TimeSpan.Zero)
                {
                    throw new TimeoutException($"Wait for loading collection {collectionName} timeout");
                }
                partitions = await milvusClient.ShowPartitionsAsync(collectionName, cancellationToken: cancellationToken);
            }
        }
    }
}