using IO.Milvus.Client;
using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.MilvusTests.Client;
using System;
using System.Collections.Generic;
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
        Verify.NotNull(milvusClient);
        if (milvusClient is MilvusRestClient)
        {
            throw new NotSupportedException("Not support restful api");
        }

        long progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken).ConfigureAwait(false);

        while (progress < 100)
        {
            await Task.Delay(waitingInterval, cancellationToken).ConfigureAwait(false);
            timeout -= waitingInterval;
            if (timeout <= TimeSpan.Zero)
            {
                throw new TimeoutException($"Wait for loading collection {collectionName} timeout");
            }

            progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken).ConfigureAwait(false);
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(milvusClient);
        if (milvusClient is MilvusRestClient)
        {
            throw new NotSupportedException("Not support restful api");
        }

        long progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken).ConfigureAwait(false);
        yield return progress;

        while (progress < 100)
        {
            await Task.Delay(waitingInterval, cancellationToken).ConfigureAwait(false);
            timeout -= waitingInterval;
            if (timeout <= TimeSpan.Zero)
            {
                throw new TimeoutException($"Wait for loading collection {collectionName} timeout");
            }

            progress = await milvusClient.GetLoadingProgressAsync(collectionName, partitionName, cancellationToken).ConfigureAwait(false);
            yield return progress;
        }
    }

    /// <summary>
    /// Wrapper methods for <see cref="IMilvusClient.GetVersionAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Return <see cref="MilvusVersion"/> instead of <see cref="string"/>.
    /// </remarks>
    /// <param name="milvusClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Milvus version</returns>
    public static async Task<MilvusVersion> GetMilvusVersionAsync(
        this IMilvusClient milvusClient,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(milvusClient);

        string version = await milvusClient.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        return MilvusVersion.Parse(version);
    }
}
