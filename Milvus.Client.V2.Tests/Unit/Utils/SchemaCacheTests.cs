using Xunit;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class SchemaCacheTests
{
    [Fact]
    public async Task GetOrLoadAsync_coalesces_concurrent_loads()
    {
        var cache = new SchemaCache(16);
        int loads = 0;
        // Block the loader on a gate so the five callers genuinely contend instead of each completing
        // synchronously (which would observe loads == 1 even without the coalescing logic).
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, 5).Select(_ => cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "coll"),
            async ct =>
            {
                Interlocked.Increment(ref loads);
                await gate.Task;
                return "schema";
            }).AsTask());

        // Give the callers time to start and block on the gate before releasing it.
        await Task.Delay(50, TestContext.Current.CancellationToken);
        gate.SetResult();

        string[] results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal("schema", r));
        Assert.Equal(1, loads);
    }

    [Fact]
    public async Task ForceUpdate_reloads_and_replaces_cached_value()
    {
        var cache = new SchemaCache(16);
        int loads = 0;
        CancellationToken ct = TestContext.Current.CancellationToken;

        string first = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "coll"),
            ct => new ValueTask<string>($"schema-{++loads}"), cancellationToken: ct);
        Assert.Equal("schema-1", first);

        // A normal hit does not reload.
        string cached = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "coll"),
            ct => new ValueTask<string>($"schema-{++loads}"), cancellationToken: ct);
        Assert.Equal("schema-1", cached);
        Assert.Equal(1, loads);

        // A forced refresh reloads and replaces the cached value.
        string refreshed = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "coll"),
            ct => new ValueTask<string>($"schema-{++loads}"),
            forceUpdate: true, cancellationToken: ct);
        Assert.Equal("schema-2", refreshed);

        // The replacement is now served from cache.
        string after = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "coll"),
            ct => new ValueTask<string>($"schema-{++loads}"), cancellationToken: ct);
        Assert.Equal("schema-2", after);
        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task Evicts_least_recently_accessed_entry_when_over_capacity()
    {
        var cache = new SchemaCache(3);
        CancellationToken ct = TestContext.Current.CancellationToken;

        await Load(cache, "a", ct);
        await Load(cache, "b", ct);
        await Load(cache, "c", ct);

        // Touch "a" so it is the most recent, making "b" the LRU victim.
        await Load(cache, "a", ct);
        await Load(cache, "d", ct);

        Assert.Equal(3, cache.Count);

        // "b" was evicted and reloads; "a" (most recently used) and "d" hit the cache.
        int loads = 0;
        string b = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "b"),
            ct => { loads++; return new ValueTask<string>("b-reloaded"); }, cancellationToken: ct);
        string a = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "a"),
            ct => { loads++; return new ValueTask<string>("a-reloaded"); }, cancellationToken: ct);
        string d = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "d"),
            ct => { loads++; return new ValueTask<string>("d-reloaded"); }, cancellationToken: ct);

        Assert.Equal("b-reloaded", b);
        Assert.Equal("a", a);
        Assert.Equal("d", d);
        Assert.Equal(1, loads);
    }

    private static async Task Load(SchemaCache cache, string collection, CancellationToken ct)
        => await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", collection),
            ct => new ValueTask<string>(collection), cancellationToken: ct);

    [Fact]
    public async Task SetCapacity_shrinks_and_evicts_lru_schemas()
    {
        var cache = new SchemaCache(5);
        CancellationToken ct = TestContext.Current.CancellationToken;

        for (int i = 1; i <= 5; i++)
        {
            await Load(cache, $"c{i}", ct);
        }

        Assert.Equal(5, cache.Count);
        cache.SetCapacity(2);
        Assert.Equal(2, cache.Count);

        // c1..c3 were loaded first and are the least recently accessed, so shrinking evicts them;
        // c4/c5 survive and are served from cache without a reload.
        int loads = 0;
        string c4 = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "c4"),
            ct => { loads++; return new ValueTask<string>("c4-reloaded"); }, cancellationToken: ct);
        string c5 = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "c5"),
            ct => { loads++; return new ValueTask<string>("c5-reloaded"); }, cancellationToken: ct);
        Assert.Equal("c4", c4);
        Assert.Equal("c5", c5);
        Assert.Equal(0, loads);

        // An evicted entry reloads on demand, confirming it was not kept.
        string c1 = await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "c1"),
            ct => { loads++; return new ValueTask<string>("c1-reloaded"); }, cancellationToken: ct);
        Assert.Equal("c1-reloaded", c1);
        Assert.Equal(1, loads);
        Assert.Equal(2, cache.Count);

        cache.SetCapacity(1);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.Count <= cache.Capacity);
    }

    [Fact]
    public async Task SetCapacity_can_grow()
    {
        var cache = new SchemaCache(2);
        CancellationToken ct = TestContext.Current.CancellationToken;

        await Load(cache, "a", ct);
        await Load(cache, "b", ct);

        cache.SetCapacity(4);
        Assert.Equal(4, cache.Capacity);
        await Load(cache, "c", ct);
        await Load(cache, "d", ct);
        Assert.Equal(4, cache.Count);
        Assert.Equal("a", await cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "a"),
            ct => new ValueTask<string>("a-reloaded"), cancellationToken: ct));
    }

    [Fact]
    public void Rejects_non_positive_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SchemaCache(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SchemaCache(-1));

        var cache = new SchemaCache(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.SetCapacity(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.SetCapacity(-1));
    }

    [Fact]
    public async Task GetOrLoadAsync_rejects_null_loader()
    {
        var cache = new SchemaCache(16);
        Func<CancellationToken, ValueTask<string>>? loader = null;
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            cache.GetOrLoadAsync(
                CollectionCacheKey.Create("localhost:19530", "default", "coll"),
                loader!,
                cancellationToken: TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Failing_loader_removes_entry_and_next_call_reloads()
    {
        var cache = new SchemaCache(16);
        CancellationToken ct = TestContext.Current.CancellationToken;
        CollectionCacheKey key = CollectionCacheKey.Create("localhost:19530", "default", "coll");

        // The loader throws; the winning caller observes the exception.
        int loads = 0;
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrLoadAsync<string>(
                key,
                ct => { loads++; throw new InvalidOperationException("boom"); },
                cancellationToken: ct).AsTask());
        Assert.Equal("boom", ex.Message);

        // The failed entry was removed (not cached as a fault), so the next call reloads instead of
        // re-throwing a stale failure -- exercising the ReferenceEquals-guarded removal.
        string result = await cache.GetOrLoadAsync(
            key,
            ct => new ValueTask<string>($"schema-{++loads}"),
            cancellationToken: ct);
        Assert.Equal("schema-2", result);
        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task Concurrent_GetOrLoad_And_SetCapacity_keeps_count_within_capacity()
    {
        var cache = new SchemaCache(64);
        CancellationToken ct = TestContext.Current.CancellationToken;
        var errors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        // Concurrent GetOrLoadAsync callers race the single-flight load completion with SetCapacity
        // shrink/grow; the write lock serializes eviction with load completion.
        var loaders = Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
        {
            try
            {
                for (int j = 0; j < 200; j++)
                {
                    string collection = $"coll-{i}-{j % 16}";
                    string schema = await cache.GetOrLoadAsync(
                        CollectionCacheKey.Create("localhost:19530", "default", collection),
                        _ => new ValueTask<string>(collection),
                        cancellationToken: ct);
                    if (schema != collection)
                    {
                        throw new InvalidOperationException($"unexpected schema for {collection}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        })).ToList();

        for (int k = 0; k < 100; k++)
        {
            cache.SetCapacity(16 + (k % 48));
        }

        await Task.WhenAll(loaders);

        Assert.Empty(errors);
        Assert.InRange(cache.Count, 0, cache.Capacity);
    }
}
