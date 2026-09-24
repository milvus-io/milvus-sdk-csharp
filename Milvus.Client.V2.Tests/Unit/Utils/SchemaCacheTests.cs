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

        var tasks = Enumerable.Range(0, 5).Select(_ => cache.GetOrLoadAsync(
            CollectionCacheKey.Create("localhost:19530", "default", "coll"),
            ct =>
            {
                Interlocked.Increment(ref loads);
                return new ValueTask<string>("schema");
            }).AsTask());

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
}
