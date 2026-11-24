namespace Milvus.Client.V2.Utils;

/// <summary>
/// Static entry points to tune the in-process SDK caches.
/// </summary>
/// <remarks>
/// The schema and collection-timestamp caches are process-wide singletons that keep a bounded LRU of
/// per-collection entries. Both default to <see cref="DefaultCapacity" /> entries and evict the
/// least-recently-accessed entry once the configured capacity is exceeded. Call <see cref="SetCapacity" />
/// before heavy workloads to raise or lower the bound; shrinking below the current size evicts entries
/// immediately.
/// <para>
/// The collection-timestamp cache backs the <c>Session</c> consistency level. Once a client tracks more
/// collections than the configured capacity, the least-recently-used timestamps are evicted and Session
/// reads for those collections fall back to eventual consistency until a later mutation stores a fresh
/// timestamp (mirroring PyMilvus, which bounds both caches at 4096).
/// </para>
/// </remarks>
public static class CacheConfig
{
    /// <summary>
    /// The default maximum number of cached entries for both caches, matching PyMilvus's
    /// <c>DEFAULT_CAPACITY</c> (4096).
    /// </summary>
    public const int DefaultCapacity = Constants.CacheDefaultCapacity;

    // Serializes SetCapacity so the two cache singletons always share one capacity: the per-cache
    // updates each hold their own lock, so without this a concurrent SetCapacity with a different
    // value could interleave and leave SchemaCache and CollectionTsCache at different capacities
    // (mirrors C++ CacheConfig's capacity_mutex).
    private static readonly object _lock = new();

    /// <summary>
    /// Sets the maximum number of cached collection schemas and timestamps for both process-wide caches.
    /// </summary>
    /// <param name="capacity">The new capacity for both caches; must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="capacity" /> is not greater
    /// than zero.</exception>
    public static void SetCapacity(int capacity)
    {
        lock (_lock)
        {
            SchemaCache.Instance.SetCapacity(capacity);
            CollectionTsCache.Instance.SetCapacity(capacity);
        }
    }

    /// <summary>
    /// Gets the current maximum number of cached collection schemas and timestamps.
    /// </summary>
    /// <remarks>
    /// Both caches share the same capacity: they default to the same value and <see cref="SetCapacity" />
    /// always configures them together, so a single value is returned (mirrors C++ <c>GetCapacity</c> /
    /// Rust <c>get_cache_capacity</c>).
    /// </remarks>
    public static int Capacity => SchemaCache.Instance.Capacity;
}
