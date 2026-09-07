using System.Collections.Concurrent;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// A process-wide cache of collection schemas, avoiding repeated <c>DescribeCollection</c> RPCs. Concurrent
/// loads for the same key are coalesced (single-flight). Mirrors the Java <c>SchemaCache</c> / C++ <c>SchemaCache</c>.
/// </summary>
internal sealed class SchemaCache
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static SchemaCache Instance { get; } = new();

    private readonly ConcurrentDictionary<CollectionCacheKey, Entry<object?>> _cache = new();

    /// <summary>
    /// Returns the cached schema for the key, or loads it once via <paramref name="loader" /> (single-flight:
    /// concurrent requests for the same key share one load).
    /// </summary>
    public async ValueTask<T> GetOrLoadAsync<T>(
        CollectionCacheKey key,
        Func<CancellationToken, ValueTask<T>> loader,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out Entry<object?>? existing))
        {
            return (T)(await existing.Task.ConfigureAwait(false))!;
        }

        var entry = new Entry<object?>();
        Entry<object?> winner = _cache.GetOrAdd(key, entry);

        if (ReferenceEquals(winner, entry))
        {
            // This call owns the load.
            try
            {
                winner.SetResult(await loader(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                winner.SetException(ex);
                _cache.TryRemove(key, out _);   // allow a later retry
                throw;
            }
        }

        return (T)(await winner.Task.ConfigureAwait(false))!;
    }

    /// <summary>
    /// Removes the cached schema for the given collection.
    /// </summary>
    public void Invalidate(string endpoint, string database, string collection)
        => _cache.TryRemove(CollectionCacheKey.Create(endpoint, database, collection), out _);

    /// <summary>
    /// Removes the cached schemas for all collections in the given database.
    /// </summary>
    public void InvalidateDb(string endpoint, string database)
    {
        CollectionCacheKey prefix = CollectionCacheKey.Create(endpoint, database, "");
        foreach (CollectionCacheKey key in _cache.Keys)
        {
            if (key.Endpoint == prefix.Endpoint && key.Database == prefix.Database)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Clears all cached schemas.
    /// </summary>
    public void Clear() => _cache.Clear();

    private sealed class Entry<T>
    {
        private readonly TaskCompletionSource<T> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<T> Task => _tcs.Task;

        public void SetResult(T value) => _tcs.TrySetResult(value);
        public void SetException(Exception ex) => _tcs.TrySetException(ex);
    }
}
