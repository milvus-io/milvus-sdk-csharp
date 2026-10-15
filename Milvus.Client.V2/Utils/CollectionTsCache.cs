using System.Collections.Concurrent;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// A process-wide cache of the per-collection DML last-timestamp, used to build the <c>guaranteeTimestamp</c> for
/// <c>ConsistencyLevel.Session</c> queries (read-your-writes within one session). Mirrors the Java
/// <c>CollectionTsCache</c> / C++ <c>CollectionTsCache</c>.
/// </summary>
public sealed class CollectionTsCache
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static CollectionTsCache Instance { get; } = new();

    private readonly ConcurrentDictionary<CollectionCacheKey, long> _cache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the last DML timestamp for the given collection, or 0 when absent.
    /// </summary>
    public long Get(string endpoint, string database, string collection)
        => _cache.TryGetValue(CollectionCacheKey.Create(endpoint, database, collection), out long ts) ? ts : 0L;

    /// <summary>
    /// Records the last DML timestamp for the given collection. A zero timestamp is ignored; the stored value is
    /// monotonic (the maximum of the existing and new timestamps).
    /// </summary>
    public void Set(string endpoint, string database, string collection, long timestamp)
    {
        if (timestamp == 0L)
        {
            return;
        }

        CollectionCacheKey key = CollectionCacheKey.Create(endpoint, database, collection);
        lock (_lock)
        {
            _cache.AddOrUpdate(key, timestamp, (_, current) => Math.Max(current, timestamp));
        }
    }

    /// <summary>
    /// Removes the cached timestamp for the given collection.
    /// </summary>
    public void Invalidate(string endpoint, string database, string collection)
        => _cache.TryRemove(CollectionCacheKey.Create(endpoint, database, collection), out _);

    /// <summary>
    /// Removes the cached timestamps for all collections in the given database.
    /// </summary>
    public void InvalidateDb(string endpoint, string database)
    {
        CollectionCacheKey prefix = CollectionCacheKey.Create(endpoint, database, "");
        lock (_lock)
        {
            foreach (CollectionCacheKey key in _cache.Keys)
            {
                if (key.Endpoint == prefix.Endpoint && key.Database == prefix.Database)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Moves the latest timestamp to a renamed collection and removes the source key.
    /// </summary>
    public void Move(string endpoint, string sourceDb, string sourceCollection, string targetDb, string targetCollection)
        => Transfer(
            CollectionCacheKey.Create(endpoint, sourceDb, sourceCollection),
            CollectionCacheKey.Create(endpoint, targetDb, targetCollection),
            dropSource: true);

    /// <summary>
    /// Copies the latest timestamp to an alias while retaining the collection key. The target is updated
    /// monotonically so a newer write through the alias is not overwritten.
    /// </summary>
    public void Copy(string endpoint, string sourceDb, string sourceCollection, string targetDb, string targetCollection)
        => Transfer(
            CollectionCacheKey.Create(endpoint, sourceDb, sourceCollection),
            CollectionCacheKey.Create(endpoint, targetDb, targetCollection),
            dropSource: false);

    private void Transfer(CollectionCacheKey source, CollectionCacheKey target, bool dropSource)
    {
        if (source == target)
        {
            return;
        }

        lock (_lock)
        {
            long latest = Math.Max(_cache.TryGetValue(source, out long s) ? s : 0L,
                _cache.TryGetValue(target, out long t) ? t : 0L);

            if (dropSource)
            {
                _cache.TryRemove(source, out _);
            }

            _cache.TryRemove(target, out _);

            if (latest != 0L)
            {
                _cache[target] = latest;
            }
        }
    }

    /// <summary>
    /// Clears all cached timestamps.
    /// </summary>
    public void Clear() => _cache.Clear();

    /// <summary>
    /// The number of cached entries.
    /// </summary>
    public int Count => _cache.Count;
}
