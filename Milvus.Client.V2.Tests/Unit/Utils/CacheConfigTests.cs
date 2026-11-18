using Xunit;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class CacheConfigTests
{
    [Fact]
    public void Default_capacity_is_4096_for_both_caches()
    {
        Assert.Equal(4096, CacheConfig.DefaultCapacity);
        Assert.Equal(4096, new SchemaCache().Capacity);
        Assert.Equal(4096, new CollectionTsCache().Capacity);
    }

    [Fact]
    public void SetCapacity_applies_to_both_caches_and_GetCapacity_reads_schema_capacity()
    {
        try
        {
            CacheConfig.SetCapacity(2048);

            Assert.Equal(2048, CacheConfig.Capacity);
            Assert.Equal(2048, SchemaCache.Instance.Capacity);
            Assert.Equal(2048, CollectionTsCache.Instance.Capacity);

            CacheConfig.SetCapacity(1024);

            Assert.Equal(1024, CacheConfig.Capacity);
            Assert.Equal(1024, SchemaCache.Instance.Capacity);
            Assert.Equal(1024, CollectionTsCache.Instance.Capacity);
        }
        finally
        {
            CacheConfig.SetCapacity(CacheConfig.DefaultCapacity);
        }
    }

    [Fact]
    public void SetCapacity_rejects_non_positive_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CacheConfig.SetCapacity(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CacheConfig.SetCapacity(-1));

        // A rejected call must not have changed either cache.
        Assert.Equal(CacheConfig.DefaultCapacity, CacheConfig.Capacity);
        Assert.Equal(CacheConfig.DefaultCapacity, SchemaCache.Instance.Capacity);
        Assert.Equal(CacheConfig.DefaultCapacity, CollectionTsCache.Instance.Capacity);
    }

    [Fact]
    public async Task Concurrent_SetCapacity_keeps_both_caches_at_one_shared_capacity()
    {
        try
        {
            // Concurrent SetCapacity calls must not interleave the two per-cache updates into divergent
            // capacities; after they settle both caches must hold the same shared value.
            var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    CacheConfig.SetCapacity(16 + ((i * 100 + j) % 48));
                }
            })).ToList();

            await Task.WhenAll(tasks);

            Assert.Equal(SchemaCache.Instance.Capacity, CollectionTsCache.Instance.Capacity);
            Assert.Equal(CollectionTsCache.Instance.Capacity, CacheConfig.Capacity);
        }
        finally
        {
            CacheConfig.SetCapacity(CacheConfig.DefaultCapacity);
        }
    }
}
