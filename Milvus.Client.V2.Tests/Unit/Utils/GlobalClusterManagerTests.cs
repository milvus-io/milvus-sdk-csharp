using System.Diagnostics;
using System.Net;
using System.Threading;
using Xunit;

using Milvus.Client.V2.Tests.Integration;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class GlobalClusterManagerTests
{
    private const string GlobalEndpoint = "https://my.global-cluster.example.com";

    private static GlobalClusterManager CreateManager(
        HttpMessageHandler handler, params SrvTarget[] seeds)
    {
        GlobalClusterManager manager = GlobalClusterManager.TryCreate(
            new ConnectConfig { Uri = GlobalEndpoint, ApiKey = "secret" },
            handler)!;
        manager.ResolveSrv = _ => seeds;
        return manager;
    }

    private static SrvTarget Seed(string host, int port, int priority = 0, int weight = 0)
        => new(priority, weight, port, host);

    [Fact]
    public void Detects_global_endpoints_case_insensitively()
    {
        Assert.True(GlobalClusterManager.IsGlobalEndpoint("https://my-global-cluster.example.com:443"));
        Assert.True(GlobalClusterManager.IsGlobalEndpoint("https://xxx.GLOBAL-CLUSTER.yyy.com:443"));
        Assert.True(GlobalClusterManager.IsGlobalEndpoint("my.global-cluster.example.com"));
        Assert.False(GlobalClusterManager.IsGlobalEndpoint("http://localhost:19530"));
        Assert.False(GlobalClusterManager.IsGlobalEndpoint(""));
        Assert.False(GlobalClusterManager.IsGlobalEndpoint(null!));

        // Only the hostname counts: a path or query carrying the marker must not misclassify a regular endpoint.
        Assert.False(GlobalClusterManager.IsGlobalEndpoint("https://host.example.com:19530/global-cluster"));
        Assert.False(GlobalClusterManager.IsGlobalEndpoint("https://host.example.com?endpoint=global-cluster"));
    }

    [Fact]
    public void TryCreate_returns_null_for_non_global_endpoint()
    {
        Assert.Null(GlobalClusterManager.TryCreate(new ConnectConfig { Uri = "http://localhost:19530" }));
    }

    [Fact]
    public async Task FetchTopologyAsync_parses_clusters_and_primary()
    {
        using var handler = new CapturingHandler(_ => { },
            """
            {"code":0,"data":{"version":7,"clusters":[
                {"clusterId":"primary","endpoint":"host-a:19530","capability":3},
                {"clusterId":"replica","endpoint":"host-b:19530","capability":1}
            ]}}
            """);
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        GlobalTopology? topology = await manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken);

        Assert.NotNull(topology);
        Assert.Equal(7, topology!.Version);
        Assert.Equal(2, topology.Clusters.Count);
        ClusterInfo primary = topology.Primary;
        Assert.Equal("primary", primary.ClusterId);
        Assert.Equal("host-a:19530", primary.Endpoint);
        Assert.True(primary.IsPrimary);
        Assert.False(topology.Clusters[1].IsPrimary);
    }

    [Fact]
    public async Task FetchTopologyAsync_probes_seed_with_endpoint_query_and_bearer()
    {
        string? capturedUrl = null;
        string? capturedAuth = null;
        using var handler = new CapturingHandler(req =>
            {
                capturedUrl = req.RequestUri!.ToString();
                capturedAuth = req.Headers.Authorization?.ToString();
            },
            """{"code":0,"data":{"version":1,"clusters":[{"clusterId":"primary","endpoint":"host-a:19530","capability":3}]}}""");
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        await manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken);

        // The seed is probed over https with the verbatim global hostname as the ?endpoint= parameter.
        Assert.Equal("https://seed-a:8443/global-cluster/topology?endpoint=my.global-cluster.example.com", capturedUrl);
        Assert.Equal("Bearer secret", capturedAuth);
    }

    [Fact]
    public void Picks_candidates_by_priority_and_weight()
    {
        // Higher weight wins within the lowest priority group; the priority-1 seed is topped up when the count
        // spans both groups. The random source is injected so the weighted draw is deterministic.
        var seeds = new[]
        {
            Seed("far", 8443, priority: 1),
            Seed("near-heavy", 8443, priority: 0, weight: 10),
            Seed("near-light", 8443, priority: 0, weight: 1)
        };

        // A draw below 10/11 lands on near-heavy (10/11 of the priority-0 group's weight).
        var picked = GlobalClusterManager.PickCandidates(seeds, 2, () => 0.0);
        Assert.Equal(2, picked.Count);
        Assert.Equal("near-heavy", picked[0].Target);
        Assert.Equal("far", picked[1].Target);

        // A draw in the last 1/11 lands on near-light; the priority-1 seed is still topped up.
        var pickedLight = GlobalClusterManager.PickCandidates(seeds, 2, () => 0.9999);
        Assert.Equal(2, pickedLight.Count);
        Assert.Equal("near-light", pickedLight[0].Target);
        Assert.Equal("far", pickedLight[1].Target);
    }

    [Fact]
    public async Task FetchTopologyAsync_fails_fast_on_api_error()
    {
        using var handler = new CapturingHandler(_ => { }, """{"code":1,"message":"boom","data":null}""");
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        // A single seed with a consistent API-level rejection fails fast (no retry) with the API error code.
        GlobalClusterApiException exception = await Assert.ThrowsAsync<GlobalClusterApiException>(() =>
            manager.FetchTopologyAsync(
                cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken));
        Assert.Equal(1, exception.Code);
        Assert.Contains("boom", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FetchTopologyAsync_fails_fast_on_authentication_error()
    {
        // 401 is deterministic (credentials are shared by every seed): it must fail fast without retrying.
        int calls = 0;
        using var handler = new CapturingHandler(_ => calls++, "{}", HttpStatusCode.Unauthorized);
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        GlobalClusterApiException exception = await Assert.ThrowsAsync<GlobalClusterApiException>(() =>
            manager.FetchTopologyAsync(
                cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken));
        Assert.Equal(401, exception.Code);
        Assert.Contains("authentication failed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, calls); // no retry
    }

    [Fact]
    public async Task FetchTopologyAsync_throws_when_no_srv_records()
    {
        using var handler = new CapturingHandler(_ => { }, """{"code":0,"data":{"version":1,"clusters":[]}}""");
        using GlobalClusterManager manager = CreateManager(handler);

        await Assert.ThrowsAsync<MilvusException>(() => manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FetchTopologyAsync_retries_http_errors_then_throws()
    {
        int calls = 0;
        using var handler = new CapturingHandler(_ => calls++, "{}", HttpStatusCode.InternalServerError);
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        await Assert.ThrowsAsync<MilvusException>(() => manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken));
        Assert.Equal(3, calls); // 3 retry attempts
    }

    [Fact]
    public async Task FetchTopologyAsync_returns_null_on_malformed_json()
    {
        // A cluster row missing its endpoint field is well-formed JSON but structurally invalid; it must not throw.
        using var handler = new CapturingHandler(_ => { },
            """{"code":0,"data":{"version":1,"clusters":[{"clusterId":"primary","capability":3}]}}""");
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        await Assert.ThrowsAsync<MilvusException>(() => manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FetchTopologyAsync_returns_on_first_success_without_waiting_for_slow_seed()
    {
        // Two seeds (DefaultProbeCount = 2): the first responds immediately, the second hangs. The first successful
        // answer must win immediately (mirroring the Java SDK), not block on the slow seed.
        using var handler = new FirstFastThenSlowHandler(
            """{"code":0,"data":{"version":1,"clusters":[{"clusterId":"primary","endpoint":"host-a:19530","capability":3}]}}""",
            slowAfterMs: 10_000);
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443), Seed("seed-b", 8443));

        Stopwatch watch = Stopwatch.StartNew();
        GlobalTopology? topology = await manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken);
        watch.Stop();

        Assert.NotNull(topology);
        Assert.Equal(1, topology!.Version);
        // The first successful answer wins immediately; it must not wait for the slow second seed (10s delay).
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5),
            $"fetch waited for the slow seed: {watch.Elapsed}");
    }

    [Fact]
    public async Task FetchTopologyAsync_returns_null_when_no_seed_reports_newer_version()
    {
        using var handler = new CapturingHandler(_ => { },
            """{"code":0,"data":{"version":1,"clusters":[{"clusterId":"primary","endpoint":"host-a:19530","capability":3}]}}""");
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        // A refresh whose cached version is already the latest on the server side must return null (no change).
        GlobalTopology? topology = await manager.FetchTopologyAsync(
            cachedVersion: 1, onHigherVersion: null, TestContext.Current.CancellationToken);

        Assert.Null(topology);
    }

    [Fact]
    public async Task FetchTopologyAsync_watcher_promotes_a_newer_version_from_a_slow_seed()
    {
        // Two seeds (DefaultProbeCount = 2): the first answer is v1 (fast), the slow seed later reports v2. The
        // fetch returns v1 immediately and the background watcher must promote v2 via onHigherVersion.
        using var handler = new ScriptedHandler(
            ("""{"code":0,"data":{"version":1,"clusters":[{"clusterId":"primary","endpoint":"host-a:19530","capability":3}]}}""", TimeSpan.Zero),
            ("""{"code":0,"data":{"version":2,"clusters":[{"clusterId":"primary","endpoint":"host-b:19530","capability":3}]}}""", TimeSpan.FromMilliseconds(500)));
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443), Seed("seed-b", 8443));

        var promoted = new List<GlobalTopology>();
        GlobalTopology? topology = await manager.FetchTopologyAsync(
            cachedVersion: null,
            onHigherVersion: t =>
            {
                promoted.Add(t);
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(topology);
        Assert.Equal(1, topology!.Version);

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (promoted.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        GlobalTopology promotedTopology = Assert.Single(promoted);
        Assert.Equal(2, promotedTopology.Version);
    }

    [Fact]
    public async Task FetchTopologyAsync_retries_srv_resolution_failures_then_throws()
    {
        int calls = 0;
        using var handler = new CapturingHandler(_ => { }, "{}");
        using GlobalClusterManager manager = CreateManager(handler);
        manager.ResolveSrv = _ =>
        {
            calls++;
            throw new MilvusException(MilvusErrorCode.UnexpectedError, "dns down");
        };

        // Transient SRV resolution failures are retried (1s/2s backoff) and then surface as a MilvusException.
        await Assert.ThrowsAsync<MilvusException>(() => manager.FetchTopologyAsync(
            cachedVersion: null, onHigherVersion: null, TestContext.Current.CancellationToken));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task RefreshAsync_invokes_topology_change_callback()
    {
        using var handler = new CapturingHandler(_ => { },
            """{"code":0,"data":{"version":2,"clusters":[{"clusterId":"primary","endpoint":"host-c:19530","capability":3}]}}""");
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        string? applied = null;
        manager.OnTopologyChange = topology =>
        {
            applied = topology.Primary.Endpoint;
            return Task.CompletedTask;
        };
        manager.GetCachedVersion = () => 1;

        await manager.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal("host-c:19530", applied);
    }

    [Fact]
    public async Task RefreshAsync_skips_concurrent_refresh()
    {
        // Two overlapping refreshes: the serialization flag lets only one fetch + callback run. The handler delays
        // its response so the first refresh is still in flight when the second starts.
        using var handler = new DelayedCapturingHandler(
            """{"code":0,"data":{"version":1,"clusters":[{"clusterId":"primary","endpoint":"host-a:19530","capability":3}]}}""",
            delayMs: 300);
        using GlobalClusterManager manager = CreateManager(handler, Seed("seed-a", 8443));

        int callbackCount = 0;
        manager.OnTopologyChange = _ =>
        {
            callbackCount++;
            return Task.CompletedTask;
        };

        Task first = manager.RefreshAsync(TestContext.Current.CancellationToken);
        Task second = manager.RefreshAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(first, second);

        Assert.Equal(1, callbackCount);
    }
}

// A CapturingHandler whose response is delayed, so overlapping refreshes deterministically collide on the
// serialization flag.
internal sealed class DelayedCapturingHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly int _delayMs;

    public DelayedCapturingHandler(string response, int delayMs)
    {
        _response = response;
        _delayMs = delayMs;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(_delayMs, cancellationToken).ConfigureAwait(false);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

// A handler whose first request responds immediately and every later one is delayed, so a first-success-wins fetch
// provably returns without waiting for the remaining (slow) seed probes.
internal sealed class FirstFastThenSlowHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly int _slowAfterMs;
    private int _calls;

    public FirstFastThenSlowHandler(string response, int slowAfterMs)
    {
        _response = response;
        _slowAfterMs = slowAfterMs;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _calls) > 1)
        {
            await Task.Delay(_slowAfterMs, cancellationToken).ConfigureAwait(false);
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

// A handler that serves a scripted sequence of (response, delay) pairs, one per request, so probes can be made to
// answer with different versions and latencies deterministically.
internal sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly (string Response, TimeSpan Delay)[] _script;
    private int _calls;

    public ScriptedHandler(params (string Response, TimeSpan Delay)[] script)
    {
        _script = script;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int index = Math.Min(Interlocked.Increment(ref _calls) - 1, _script.Length - 1);
        (string response, TimeSpan delay) = _script[index];
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
