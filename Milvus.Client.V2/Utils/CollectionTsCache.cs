using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// A process-wide, capacity-bounded LRU cache of the per-collection DML last-timestamp, used to build the
/// <c>guaranteeTimestamp</c> for <c>ConsistencyLevel.Session</c> queries (read-your-writes within one session).
/// Mirrors the Java <c>CollectionTsCache</c> / C++ <c>CollectionTsCache</c> and PyMilvus's <c>CollectionTsCache</c>
/// (an LRU cache capped at 4096 entries by default).
/// </summary>
// Process-lifetime reader-writer lock on a process-wide singleton cache; the shared Instance is
// never disposed, so a formal IDisposable would only exist to satisfy CA1001 and would force every
// short-lived test instance to be disposed.
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "Process-lifetime lock on a never-disposed singleton cache.")]
internal sealed class CollectionTsCache
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static CollectionTsCache Instance { get; } = new();

    private readonly ConcurrentDictionary<CollectionCacheKey, Entry> _cache = new();

    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private long _accessSequence;
    private volatile int _capacity;

    /// <summary>
    /// Creates a new cache with the default capacity (<see cref="Constants.CacheDefaultCapacity" />).
    /// </summary>
    public CollectionTsCache()
        : this(Constants.CacheDefaultCapacity)
    {
    }

    /// <summary>
    /// Creates a new cache with the given capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of entries before the least-recently-used entry is evicted.
    /// Must be greater than zero.</param>
    public CollectionTsCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Cache capacity must be greater than zero");
        }

        _capacity = capacity;
    }

    /// <summary>
    /// The current maximum number of cached entries. Use <see cref="SetCapacity" /> to change it; shrinking evicts the
    /// least-recently-accessed entries.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Sets the cache capacity, evicting least-recently-used entries as needed.
    /// </summary>
    /// <param name="newCapacity">The new maximum number of cached entries. Must be greater than zero.</param>
    public void SetCapacity(int newCapacity)
    {
        if (newCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newCapacity), "Cache capacity must be greater than zero");
        }

        _lock.EnterWriteLock();
        try
        {
            _capacity = newCapacity;
            while (_cache.Count > _capacity)
            {
                EvictLeastRecentlyUsed();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the last DML timestamp for the given collection, or 0 when absent. Touches the entry so it counts
    /// as most-recently used.
    /// </summary>
    public long Get(string endpoint, string database, string collection)
    {
        CollectionCacheKey key = CollectionCacheKey.Create(endpoint, database, collection);
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out Entry? entry))
            {
                Touch(entry);
                return entry.Timestamp;
            }

            return 0L;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

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
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out Entry? entry))
            {
                if (timestamp > entry.Timestamp)
                {
                    entry.Timestamp = timestamp;
                }

                Touch(entry);
                return;
            }

            _cache[key] = new Entry(timestamp, NextAccess());
            EvictIfNeeded();
        }
        finally
        {
            _lock.ExitWriteLock();
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
        _lock.EnterWriteLock();
        try
        {
            foreach (CollectionCacheKey key in _cache.Keys)
            {
                if (key.Endpoint == prefix.Endpoint && key.Database == prefix.Database)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
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

        _lock.EnterWriteLock();
        try
        {
            long latest = Math.Max(
                _cache.TryGetValue(source, out Entry? sourceEntry) ? sourceEntry.Timestamp : 0L,
                _cache.TryGetValue(target, out Entry? targetEntry) ? targetEntry.Timestamp : 0L);

            if (dropSource)
            {
                _cache.TryRemove(source, out _);
            }
            else if (sourceEntry is { } sourceCopy)
            {
                // An alias copy keeps the source; bump its recency so inserting the alias target cannot
                // evict the canonical collection it just aliased, silently dropping its SESSION
                // timestamp (mirrors Java's touch of the source in the copy path).
                Touch(sourceCopy);
            }

            _cache.TryRemove(target, out _);

            if (latest != 0L)
            {
                _cache[target] = new Entry(latest, NextAccess());
                EvictIfNeeded();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
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

    private long NextAccess() => Interlocked.Increment(ref _accessSequence);

    private void Touch(Entry entry)
    {
        long access = Interlocked.Increment(ref _accessSequence);
        long previous = Volatile.Read(ref entry.LastAccess);
        while (previous < access
               && Interlocked.CompareExchange(ref entry.LastAccess, access, previous) != previous)
        {
            previous = Volatile.Read(ref entry.LastAccess);
        }
    }

    private void EvictIfNeeded()
    {
        if (_cache.Count <= _capacity)
        {
            return;
        }

        EvictLeastRecentlyUsed();
    }

    private void EvictLeastRecentlyUsed()
    {
        CollectionCacheKey? victim = null;
        long oldest = long.MaxValue;
        foreach (KeyValuePair<CollectionCacheKey, Entry> pair in _cache)
        {
            long lastAccess = Volatile.Read(ref pair.Value.LastAccess);
            if (lastAccess < oldest)
            {
                oldest = lastAccess;
                victim = pair.Key;
            }
        }

        if (victim is { } key)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private sealed class Entry
    {
        public Entry(long timestamp, long lastAccess)
        {
            Timestamp = timestamp;
            LastAccess = lastAccess;
        }

        public long Timestamp;
        public long LastAccess;
    }
}
