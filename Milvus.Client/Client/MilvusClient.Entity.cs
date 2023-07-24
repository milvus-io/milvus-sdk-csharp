using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Maps collection names to their last known mutation timestamp.
    /// Used to implement <see cref="ConsistencyLevel.Session" />.
    /// </summary>
    internal ConcurrentDictionary<string, ulong> CollectionLastMutationTimestamps { get; } = new();

    /// <summary>
    /// Get the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">Segment ids</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>If segments flushed.</returns>
    public async Task<bool> GetFlushStateAsync(
        IList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        GetFlushStateRequest request = new();
        request.SegmentIDs.AddRange(segmentIds);

        GetFlushStateResponse response =
            await InvokeAsync(GrpcClient.GetFlushStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Flushed;
    }
}
