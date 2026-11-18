using Xunit;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class CollectionTsCacheTests
{
    [Fact]
    public void Default_capacity_is_4096_and_bounds_entries()
    {
        Assert.Equal(4096, new CollectionTsCache().Capacity);

        var cache = new CollectionTsCache(2);
        cache.Set("host:19530", "db", "a", 1L);
        cache.Set("host:19530", "db", "b", 2L);
        cache.Set("host:19530", "db", "c", 3L);

        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Set_keeps_latest_timestamp_and_isolates_entries()
    {
        var cache = new CollectionTsCache();

        cache.Set("host:19530", "", "coll", 20L);
        cache.Set("host:19530", "default", "coll", 10L);
        cache.Set("host:19531", "default", "coll", 30L);
        cache.Set("host:19530", "other", "coll", 40L);
        cache.Set("https://cloud.example.com", "default", "coll", 50L);

        Assert.Equal(20L, cache.Get("http://host:19530", "default", "coll"));
        Assert.Equal(30L, cache.Get("host:19531", "default", "coll"));
        Assert.Equal(40L, cache.Get("host:19530", "other", "coll"));
        Assert.Equal(50L, cache.Get("cloud.example.com:19530", "default", "coll"));
        Assert.Equal(4, cache.Count);
    }

    [Fact]
    public void Set_ignores_zero_and_keeps_monotonic_timestamp()
    {
        var cache = new CollectionTsCache();

        cache.Set("host:19530", "db", "coll", 0L);
        Assert.Equal(0, cache.Count);

        cache.Set("host:19530", "db", "coll", 100L);
        cache.Set("host:19530", "db", "coll", 50L);
        Assert.Equal(100L, cache.Get("host:19530", "db", "coll"));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Move_transfers_timestamp_across_databases()
    {
        var cache = new CollectionTsCache();

        cache.Set("host:19530", "source", "old", 100L);
        cache.Set("host:19530", "target", "new", 50L);
        cache.Move("host:19530", "source", "old", "target", "new");

        Assert.Equal(0L, cache.Get("host:19530", "source", "old"));
        Assert.Equal(100L, cache.Get("host:19530", "target", "new"));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Copy_keeps_collection_entry_and_preserves_newer_alias_timestamp()
    {
        var cache = new CollectionTsCache();

        cache.Set("host:19530", "db", "collection", 100L);
        cache.Set("host:19530", "db", "alias", 50L);
        cache.Copy("host:19530", "db", "collection", "db", "alias");

        Assert.Equal(100L, cache.Get("host:19530", "db", "collection"));
        Assert.Equal(100L, cache.Get("host:19530", "db", "alias"));

        cache.Set("host:19530", "db", "alias", 200L);
        cache.Copy("host:19530", "db", "collection", "db", "alias");
        Assert.Equal(200L, cache.Get("host:19530", "db", "alias"));
    }

    [Fact]
    public void InvalidateDb_removes_only_matching_database()
    {
        var cache = new CollectionTsCache();

        cache.Set("host:19530", "db", "one", 1L);
        cache.Set("host:19530", "db", "two", 2L);
        cache.Set("host:19530", "other", "four", 4L);

        cache.InvalidateDb("host:19530", "db");

        Assert.Equal(4L, cache.Get("host:19530", "other", "four"));
        Assert.Equal(0L, cache.Get("host:19530", "db", "one"));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Evicts_least_recently_accessed_entry_when_over_capacity()
    {
        var cache = new CollectionTsCache(3);

        cache.Set("host:19530", "db", "a", 1L);
        cache.Set("host:19530", "db", "b", 2L);
        cache.Set("host:19530", "db", "c", 3L);

        // Touch "b" so it is the most recent, making "a" the LRU victim.
        Assert.Equal(2L, cache.Get("host:19530", "db", "b"));
        cache.Set("host:19530", "db", "d", 4L);

        Assert.Equal(3, cache.Count);
        Assert.Equal(0L, cache.Get("host:19530", "db", "a"));
        Assert.Equal(2L, cache.Get("host:19530", "db", "b"));
        Assert.Equal(3L, cache.Get("host:19530", "db", "c"));
        Assert.Equal(4L, cache.Get("host:19530", "db", "d"));
    }

    [Fact]
    public void SetCapacity_shrinks_and_evicts_lru_entries()
    {
        var cache = new CollectionTsCache(5);

        for (int i = 1; i <= 5; i++)
        {
            cache.Set("host:19530", "db", $"c{i}", i);
        }

        Assert.Equal(5, cache.Count);
        cache.SetCapacity(2);
        Assert.Equal(2, cache.Count);

        // c1..c3 were inserted first and are the least recently accessed, so shrinking evicts them.
        Assert.Equal(0L, cache.Get("host:19530", "db", "c1"));
        Assert.Equal(0L, cache.Get("host:19530", "db", "c2"));
        Assert.Equal(0L, cache.Get("host:19530", "db", "c3"));
        Assert.Equal(4L, cache.Get("host:19530", "db", "c4"));
        Assert.Equal(5L, cache.Get("host:19530", "db", "c5"));

        cache.SetCapacity(1);
        Assert.Equal(1, cache.Count);
        Assert.Equal(5L, cache.Get("host:19530", "db", "c5"));
        Assert.Equal(0L, cache.Get("host:19530", "db", "c4"));
    }

    [Fact]
    public async Task Concurrent_Set_Get_SetCapacity_keeps_count_within_capacity()
    {
        var cache = new CollectionTsCache(64);
        var errors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < 1000; j++)
                {
                    string collection = $"coll-{i}-{j % 16}";
                    cache.Set("host:19530", "db", collection, j + 1);
                    cache.Get("host:19530", "db", collection);
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        })).ToList();

        // Shrink and grow the capacity while the writers are running; shrinking evicts
        // least-recently-accessed entries under _lock, and growing leaves them intact.
        for (int k = 0; k < 100; k++)
        {
            cache.SetCapacity(16 + (k % 48));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(errors);
        Assert.InRange(cache.Count, 0, cache.Capacity);
    }

    [Fact]
    public void SetCapacity_can_grow()
    {
        var cache = new CollectionTsCache(2);

        cache.Set("host:19530", "db", "a", 1L);
        cache.Set("host:19530", "db", "b", 2L);

        cache.SetCapacity(4);
        Assert.Equal(4, cache.Capacity);
        cache.Set("host:19530", "db", "c", 3L);
        cache.Set("host:19530", "db", "d", 4L);
        Assert.Equal(4, cache.Count);
        Assert.Equal(1L, cache.Get("host:19530", "db", "a"));
    }

    [Fact]
    public void Rejects_non_positive_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollectionTsCache(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollectionTsCache(-1));

        var cache = new CollectionTsCache(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.SetCapacity(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.SetCapacity(-1));
    }
}
