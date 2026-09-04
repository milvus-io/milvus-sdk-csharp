using System.Collections.Concurrent;
using Milvus.Client.V2.Responses.Collection;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// A process-wide cache of collection schemas, avoiding repeated <c>DescribeCollection</c> RPCs. Concurrent
/// loads for the same key are coalesced (single-flight). Mirrors the Java <c>SchemaCache</c> / C++ <c>SchemaCache</c>.
/// </summary>
internal sealed class SchemaCache
{
    /// <summary>
    /// The default maximum number of cached entries, matching the Java SDK's <c>DEFAULT_CAPACITY</c> (4096).
    /// When the cache exceeds this bound, the least-recently-accessed entry is evicted (LRU).
    /// </summary>
    public const int DefaultCapacity = 4096;

    /// <summary>
    /// The shared instance.
    /// </summary>
    public static SchemaCache Instance { get; } = new();

    private readonly ConcurrentDictionary<CollectionCacheKey, Entry<object?>> _cache = new();
    private readonly object _lock = new();
    private readonly int _capacity;
    private long _accessSequence;

    /// <summary>
    /// Creates a new cache with the default capacity (<see cref="DefaultCapacity" />).
    /// </summary>
    public SchemaCache()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Creates a new cache with the given capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of entries before the least-recently-used entry is evicted.
    /// Must be greater than zero.</param>
    public SchemaCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Cache capacity must be greater than zero");
        }

        _capacity = capacity;
    }

    /// <summary>
    /// Returns the cached schema for the key, or loads it once via <paramref name="loader" /> (single-flight:
    /// concurrent requests for the same key share one load).
    /// </summary>
    /// <param name="key">The cache key identifying the collection.</param>
    /// <param name="loader">The delegate that loads the schema when it is not cached (or when
    /// <paramref name="forceUpdate" /> is set).</param>
    /// <param name="forceUpdate">When <c>true</c>, bypasses the cached entry and re-loads the schema, replacing
    /// the cached value. Used to recover from a collection recreate or a server-side <c>SchemaMismatch</c>.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async ValueTask<T> GetOrLoadAsync<T>(
        CollectionCacheKey key,
        Func<CancellationToken, ValueTask<T>> loader,
        bool forceUpdate = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceUpdate && _cache.TryGetValue(key, out Entry<object?>? existing))
        {
            Touch(existing);
            return (T)(await WaitAsync(existing.Task, cancellationToken).ConfigureAwait(false))!;
        }

        var entry = new Entry<object?>();
        Entry<object?> winner = forceUpdate
            ? _cache.AddOrUpdate(key, entry, (_, _) => entry)
            : _cache.GetOrAdd(key, entry);

        if (ReferenceEquals(winner, entry))
        {
            Touch(winner);

            // This call owns the load. Run it without the winning caller's token: a cancel from any single
            // caller must not tear down the shared load that all concurrent callers are waiting on (the same
            // pattern as the shared connect task).
            try
            {
                winner.SetResult(await loader(CancellationToken.None).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                winner.SetException(ex);
                // Allow a later retry, but only remove the entry if it is still ours: a concurrent rename
                // (Move) relocates this entry to a new key, and a fresh load of the original name may already
                // have replaced it -- evicting that live entry would strand its waiters forever.
                if (_cache.TryGetValue(key, out Entry<object?>? current) && ReferenceEquals(current, winner))
                {
                    _cache.TryRemove(key, out _);
                }

                throw;
            }

            EvictIfNeeded();
        }

        return (T)(await WaitAsync(winner.Task, cancellationToken).ConfigureAwait(false))!;
    }

    // Bounds the cache at _capacity entries, evicting the least-recently-accessed entry (LRU), mirroring the
    // Java/C++ SchemaCache capacity handling. Called after a new entry completes its load.
    private void EvictIfNeeded()
    {
        if (_cache.Count <= _capacity)
        {
            return;
        }

        lock (_lock)
        {
            if (_cache.Count <= _capacity)
            {
                return;
            }

            CollectionCacheKey? victim = null;
            long oldest = long.MaxValue;
            foreach (KeyValuePair<CollectionCacheKey, Entry<object?>> pair in _cache)
            {
                if (pair.Value.LastAccess < oldest)
                {
                    oldest = pair.Value.LastAccess;
                    victim = pair.Key;
                }
            }

            if (victim is { } key)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    // Task.WaitAsync is not available on netstandard2.0/net462; emulate it by racing the task against a
    // cancel-only delay so the caller's token still surfaces a cancellation promptly.
    private static async Task<T> WaitAsync<T>(Task<T> task, CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
        {
            TaskCompletionSource<bool> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                Task completed = await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false);
                if (completed == cancelled.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the cached schema for the given collection.
    /// </summary>
    public void Invalidate(string endpoint, string database, string collection)
        => _cache.TryRemove(CollectionCacheKey.Create(endpoint, database, collection), out _);

    /// <summary>
    /// Returns the canonical collection name that the given (possibly alias) key's cached schema resolves to,
    /// when the entry is present and completed. Used to invalidate the canonical-name cache entry when a
    /// drop/schema mutation is issued via an alias.
    /// </summary>
    public bool TryGetResolvedCollectionName(string endpoint, string database, string collectionName, out string canonical)
    {
        CollectionCacheKey key = CollectionCacheKey.Create(endpoint, database, collectionName);
        if (_cache.TryGetValue(key, out Entry<object?>? entry)
            && entry.Task.Status == TaskStatus.RanToCompletion
            && entry.Task.Result is DescribeCollectionResp resp)
        {
            canonical = resp.CollectionName;
            return !string.IsNullOrEmpty(canonical) && canonical != collectionName;
        }

        canonical = "";
        return false;
    }

    /// <summary>
    /// Returns the aliases (other than <paramref name="collectionName" />) that have a cached schema entry
    /// describing <paramref name="collectionName" /> -- i.e. alias keys populated by describing an alias, whose
    /// cached <see cref="DescribeCollectionResp.CollectionName" /> resolves back to the canonical collection.
    /// Used to invalidate alias-keyed cache entries when the canonical collection is dropped or its schema is
    /// mutated.
    /// </summary>
    public List<string> GetAliasKeys(string endpoint, string database, string collectionName)
    {
        CollectionCacheKey prefix = CollectionCacheKey.Create(endpoint, database, "");
        var aliases = new List<string>();
        foreach (KeyValuePair<CollectionCacheKey, Entry<object?>> pair in _cache)
        {
            if (pair.Key.Endpoint != prefix.Endpoint || pair.Key.Database != prefix.Database
                || pair.Key.Collection == collectionName)
            {
                continue;
            }

            if (pair.Value.Task.Status == TaskStatus.RanToCompletion
                && pair.Value.Task.Result is DescribeCollectionResp resp
                && resp.CollectionName == collectionName)
            {
                aliases.Add(pair.Key.Collection);
            }
        }

        return aliases;
    }

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
    /// Moves the cached schema for the given collection to a new database/collection name (used on rename).
    /// </summary>
    public void Move(string endpoint, string oldDatabase, string oldCollection, string newDatabase, string newCollection)
    {
        CollectionCacheKey oldKey = CollectionCacheKey.Create(endpoint, oldDatabase, oldCollection);
        if (_cache.TryRemove(oldKey, out Entry<object?>? entry))
        {
            _cache[CollectionCacheKey.Create(endpoint, newDatabase, newCollection)] = entry;
        }
    }

    /// <summary>
    /// Clears all cached schemas.
    /// </summary>
    public void Clear() => _cache.Clear();

    /// <summary>
    /// The number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    private void Touch(Entry<object?> entry)
    {
        lock (_lock)
        {
            entry.LastAccess = ++_accessSequence;
        }
    }

    private sealed class Entry<T>
    {
        private readonly TaskCompletionSource<T> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<T> Task => _tcs.Task;

        public long LastAccess { get; set; }

        public void SetResult(T value) => _tcs.TrySetResult(value);
        public void SetException(Exception ex) => _tcs.TrySetException(ex);
    }
}
