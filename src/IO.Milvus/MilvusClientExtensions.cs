using IO.Milvus.Client;
using IO.Milvus.Client.REST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus;

/// <summary>
/// Extension methods for <see cref="Client.IMilvusClient"/>
/// </summary>
public static class MilvusClientExtensions
{
    /// <summary>
    /// Use <see cref="IMilvusClient.ShowCollectionsAsync(IList{string}, ShowType, CancellationToken)"/> to check loading percentages of the collection.
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
    /// <exception cref="InvalidOperationException">Collection not found in milvus.</exception>
    /// <exception cref="TimeoutException">Time out.</exception>
    [Obsolete("This method will be removed in the next version. Please use WaitForLoadingCollectionAsync instead.")]
    public static async Task WaitForLoadingCollectionAsync(
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

    /// <summary>
    /// Use <see cref="IMilvusClient.GetLoadingProgressAsync(string, IList{string}, CancellationToken)"/> to check loading percentages of the collection.
    /// </summary>
    /// <remarks>
    /// Not support restful api.Only <see cref="Client.gRPC.MilvusGrpcClient"/>
    /// </remarks>
    /// <param name="milvusClient">Milvus client.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="waitingInterval">Waiting interval.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">When you are using <see cref="MilvusRestClient"/>.</exception>
    /// <exception cref="TimeoutException">Time out.</exception>
    public static async Task WaitForLoadingProgressCollectionAsync(
        this IMilvusClient milvusClient,
        string collectionName,
        IList<string> partitionName,
        TimeSpan waitingInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (milvusClient is MilvusRestClient)
        {
            throw new NotSupportedException("Not support restful api");
        }

        long progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken);

        while (progress < 100)
        {
            await Task.Delay(waitingInterval, cancellationToken);
            timeout -= waitingInterval;
            if (timeout <= TimeSpan.Zero)
            {
                throw new TimeoutException($"Wait for loading collection {collectionName} timeout");
            }

            progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken);
        }
    }

    /// <summary>
    /// Use <see cref="IMilvusClient.GetLoadingProgressAsync(string, IList{string}, CancellationToken)"/> to check loading percentages of the collection and yield value.
    /// </summary>
    /// <remarks>
    /// Not support restful api.Only <see cref="Client.gRPC.MilvusGrpcClient"/>
    /// </remarks>
    /// <param name="milvusClient">Milvus client.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="waitingInterval">Waiting interval.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">When you are using <see cref="MilvusRestClient"/>.</exception>
    /// <exception cref="TimeoutException">Time out.</exception>
    public static async IAsyncEnumerable<long> WaitForLoadingProgressCollectionValueAsync(
        this IMilvusClient milvusClient,
        string collectionName,
        IList<string> partitionName,
        TimeSpan waitingInterval,
        TimeSpan timeout,
        [EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        if (milvusClient is MilvusRestClient)
        {
            throw new NotSupportedException("Not support restful api");
        }

        var progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken);
        yield return progress;

        while (progress < 100)
        {
            await Task.Delay(waitingInterval, cancellationToken);
            timeout -= waitingInterval;
            if (timeout <= TimeSpan.Zero)
            {
                throw new TimeoutException($"Wait for loading collection {collectionName} timeout");
            }

            progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken);
            yield return progress;
        }
    }
}