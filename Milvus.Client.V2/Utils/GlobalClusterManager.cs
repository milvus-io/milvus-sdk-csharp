using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

using DnsClient;
using DnsClient.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Detects global-cluster (Zilliz Cloud) endpoints, discovers the ha-manager seeds via
/// <c>_grpc._tcp.</c> SRV records, probes them concurrently for the member-cluster topology, and manages primary
/// discovery / failover for a <see cref="MilvusClientV2" />. Mirrors the Java SDK's <c>GlobalClusterUtils</c> /
/// <c>GlobalStub</c> / <c>TopologyRefresher</c> (including the SRV-based seed discovery added in the Java SDK #2099).
/// </summary>
/// <remarks>
/// Zilliz Cloud only. A global endpoint is a logical URI whose hostname contains <c>global-cluster</c>. Its
/// <c>_grpc._tcp.</c> SRV records resolve to ha-manager seeds; each seed serves the REST
/// <c>/global-cluster/topology?endpoint=&lt;hostname&gt;</c> endpoint describing the member clusters. The writable
/// (primary) cluster's gRPC endpoint is what a <see cref="MilvusClientV2" /> actually talks to. The topology is
/// re-fetched on a fixed interval and reactively on connectivity failures (gRPC <c>UNAVAILABLE</c> errors and
/// server-side global-cluster routing errors), so a primary change fails the client over to the new primary.
/// </remarks>
internal sealed class GlobalClusterManager : IDisposable
{
    private const string GlobalClusterMarker = "global-cluster";
    private const string TopologyPath = "global-cluster/topology";
    private const string SrvServicePrefix = "_grpc._tcp.";
    private const string EndpointQueryParam = "endpoint";

    // Number of seeds probed concurrently on the first fetch (nothing cached yet), spanning priorities so a slow or
    // dead nearest region does not stall discovery. Refresh paths (a version is already cached) probe every seed.
    private const int DefaultProbeCount = 2;

    // Refresh cadence, UNAVAILABLE probe debounce and the grace period for lagging seeds, matching the Java SDK
    // (5 min, 5 s, 1 s).
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UnavailableDebounce = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProbeGracePeriod = TimeSpan.FromMilliseconds(1000);

    private readonly string _globalEndpoint;
    private readonly string _globalHostname;
    private readonly HttpClient _http;
    private readonly ILogger _log;
    private readonly object _sync = new();
    private Timer? _refreshTimer;
    private DateTime _lastUnavailableProbeUtc = DateTime.MinValue;
    private bool _disposed;

    // 1 when a refresh is in flight, 0 otherwise; serializes the 5-minute timer refresh against the
    // UNAVAILABLE-triggered probe so two overlapping fetches cannot interleave (mirroring the Java
    // TopologyRefresher's single-thread scheduler + refreshing flag).
    private int _refreshing;

    // Test seam: resolves the _grpc._tcp SRV records for a hostname. Defaults to a real DnsClient query; tests
    // inject a canned seed list so no DNS is needed.
    internal Func<string, IReadOnlyList<SrvTarget>>? ResolveSrv { get; set; }

    // Returns the version of the topology currently applied by the client, if any. Used by the periodic / UNAVAILABLE
    // refresh to probe every seed and gate on a strictly newer version (mirroring the Java TopologyRefresher's
    // getCurrent().getVersion()).
    internal Func<long?>? GetCachedVersion { get; set; }

    /// <summary>
    /// Raised after a topology refresh fetches a topology; the receiver applies it (e.g. fails over when the primary
    /// changed). Mirrors the Java onTopologyChange predicate.
    /// </summary>
    internal Func<GlobalTopology, Task>? OnTopologyChange { get; set; }

    private GlobalClusterManager(
        string globalEndpoint, string? bearerToken, HttpMessageHandler? handler, ILogger log)
    {
        _globalEndpoint = globalEndpoint;
        _globalHostname = EndpointHostname(globalEndpoint);
        _log = log;

        // An injected handler (tests) is owned by the caller, so pass disposeHandler: false; the default handler is
        // created here and owned (and disposed) by the HttpClient.
#pragma warning disable CA2000 // HttpClient takes ownership of the default handler and disposes it with itself.
        _http = handler is null
            ? new HttpClient(CreateDefaultHandler()) { Timeout = TimeSpan.FromSeconds(10) }
            : new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(10) };
#pragma warning restore CA2000

        if (bearerToken is not null)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    // The default handler is owned by the HttpClient (disposed with it). An injected handler (tests) is owned by
    // the caller. The return type is intentionally the base type: netstandard2.0/net462 only have HttpClientHandler,
    // while net8.0 uses SocketsHttpHandler.
#pragma warning disable CA1859 // The handler type varies by target framework.
    private static HttpMessageHandler CreateDefaultHandler()
    {
#if NET5_0_OR_GREATER
        return new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(10) };
#else
        // netstandard2.0 / net462 have no SocketsHttpHandler; the default handler applies no connect timeout, but
        // HttpClient.Timeout still bounds the whole request.
        return new HttpClientHandler();
#endif
    }
#pragma warning restore CA1859

    /// <summary>
    /// Creates a manager for the given global endpoint, or <c>null</c> when <paramref name="config" />'s URI is not a
    /// global cluster endpoint (its hostname does not contain <c>global-cluster</c>).
    /// </summary>
    internal static GlobalClusterManager? TryCreate(ConnectConfig config, HttpMessageHandler? handler = null)
    {
        if (!IsGlobalEndpoint(config.Uri))
        {
            return null;
        }

        // The topology REST API authenticates with a Bearer token: the API key when set, otherwise username:password
        // (matching the Java SDK's ConnectConfig.getAuthorization()).
        string? bearer = config.ApiKey ?? (config.Username is null ? null : $"{config.Username}:{config.Password}");
        ILogger log = config.LoggerFactory?.CreateLogger("Milvus.Client.V2") ?? NullLogger.Instance;
        return new GlobalClusterManager(config.Uri, bearer, handler, log);
    }

    internal static bool IsGlobalEndpoint(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        // Only the hostname decides, matching the documented rule: a regular endpoint whose path, query string or
        // port happens to contain the marker must not be misclassified as a global endpoint.
        string value = uri.Trim();
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = "https://" + value;
        }

        try
        {
            string hostname = new Uri(value).Host;
            // netstandard2.0/net462 have no string.Contains(string, StringComparison); IndexOf is the shared spelling.
#pragma warning disable CA2249 // Consider using string.Contains for readability
            return hostname.IndexOf(GlobalClusterMarker, StringComparison.OrdinalIgnoreCase) >= 0;
#pragma warning restore CA2249
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    // Extracts the bare hostname from a global cluster endpoint, used for the SRV query name and the verbatim
    // ?endpoint= value (mirroring the Java SDK's endpointHostname).
    internal static string EndpointHostname(string globalEndpoint)
    {
        string endpoint = globalEndpoint.Trim();
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = "https://" + endpoint;
        }

        try
        {
            string hostname = new Uri(endpoint).Host;
            if (string.IsNullOrEmpty(hostname))
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, "Invalid global cluster endpoint: " + globalEndpoint);
            }

            return hostname;
        }
        catch (UriFormatException ex)
        {
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, "Invalid global cluster endpoint: " + globalEndpoint, ex);
        }
    }

    // Resolves the _grpc._tcp SRV records via DnsClient.NET (the .NET standard library has no SRV query support).
    // Returns an empty list for NXDOMAIN / no SRV records (a deterministic "not provisioned" state); transient DNS
    // failures are raised.
    private static IReadOnlyList<SrvTarget> ResolveSrvDefault(string hostname)
    {
        var lookup = new LookupClient();
        IDnsQueryResponse response = lookup.Query(SrvServicePrefix + hostname, QueryType.SRV);
        if (response.HasError)
        {
            if (string.Equals(response.ErrorMessage, "Non-Existent Domain", StringComparison.OrdinalIgnoreCase)
                || response.Answers.Count == 0)
            {
                return Array.Empty<SrvTarget>();
            }

            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, $"SRV resolution failed for '{SrvServicePrefix}{hostname}': {response.ErrorMessage}");
        }

        var targets = new List<SrvTarget>();
        foreach (DnsResourceRecord answer in response.Answers)
        {
            if (answer is not SrvRecord srv)
            {
                continue;
            }

            string target = srv.Target.ToString().TrimEnd('.');
            if (target.Length == 0)
            {
                continue;
            }

            targets.Add(new SrvTarget(srv.Priority, srv.Weight, srv.Port, target));
        }

        return SortSrvTargets(targets);
    }

    internal IReadOnlyList<SrvTarget> ResolveSeeds(string hostname)
        => ResolveSrv?.Invoke(hostname) ?? ResolveSrvDefault(hostname);

    // Sorts SRV targets nearest first: lower priority wins, higher weight breaks ties (RFC 2782).
    internal static List<SrvTarget> SortSrvTargets(List<SrvTarget> targets)
    {
        targets.Sort((a, b) =>
        {
            int byPriority = a.Priority.CompareTo(b.Priority);
            return byPriority != 0 ? byPriority : b.Weight.CompareTo(a.Weight);
        });
        return targets;
    }

    // Selects seeds to probe: one weighted pick per priority group (lowest/nearest first), then topped up with
    // weighted picks without replacement from all remaining targets (mirroring the Java SDK's pickCandidates).
    internal static List<SrvTarget> PickCandidates(IReadOnlyList<SrvTarget> targets, int count)
        => PickCandidates(targets, count, RandomInstance.NextDouble);

    // Test seam mirroring the Java SDK's pickCandidates(targets, count, random): the production overload draws from
    // the shared RandomInstance, while tests inject a deterministic random source.
    internal static List<SrvTarget> PickCandidates(
        IReadOnlyList<SrvTarget> targets, int count, Func<double> randomSource)
    {
        var picked = new List<SrvTarget>();
        if (targets.Count == 0 || count <= 0)
        {
            return picked;
        }

        var remaining = new List<SrvTarget>();
        foreach (SrvTarget target in targets)
        {
            if (!remaining.Contains(target))
            {
                remaining.Add(target);
            }
        }

        foreach (IGrouping<int, SrvTarget> group in remaining.GroupBy(t => t.Priority).OrderBy(g => g.Key))
        {
            SrvTarget choice = WeightedChoice(group.ToList(), randomSource);
            picked.Add(choice);
            remaining.Remove(choice);
            if (picked.Count >= count)
            {
                return picked;
            }
        }

        while (picked.Count < count && remaining.Count > 0)
        {
            SrvTarget choice = WeightedChoice(remaining, randomSource);
            picked.Add(choice);
            remaining.Remove(choice);
        }

        return picked;
    }

    // Jitter/weighted choice randomness is deliberately non-cryptographic (seed selection / retry staggering only),
    // so CA5394 does not apply. Random.Shared is unavailable on netstandard2.0/net462, so use a static instance.
#pragma warning disable CA5394 // Random is an insecure random number generator.
    private static readonly Random RandomInstance = new();

    private static SrvTarget WeightedChoice(List<SrvTarget> group, Func<double> randomSource)
    {
        long total = group.Sum(t => Math.Max(t.Weight, 0));
        if (total == 0)
        {
            return group[(int)(randomSource() * group.Count)];
        }

        long threshold = (long)(randomSource() * total);
        foreach (SrvTarget target in group)
        {
            threshold -= Math.Max(target.Weight, 0);
            if (threshold < 0)
            {
                return target;
            }
        }

        return group[group.Count - 1];
    }
#pragma warning restore CA5394

    // Builds the seed topology URL with the verbatim ?endpoint=<hostname> query parameter (the server derives the
    // global cluster id from its first DNS label), mirroring the Java SDK's buildSeedTopologyUrl.
    private static string BuildSeedTopologyUrl(SrvTarget seed, string hostname)
        => $"https://{seed.Target}:{seed.Port}/{TopologyPath}?{EndpointQueryParam}={hostname}";

    /// <summary>
    /// Fetches the current global-cluster topology by probing the ha-manager seeds concurrently. Returns <c>null</c>
    /// when every probe failed after retries or no seed reports a strictly newer version than
    /// <paramref name="cachedVersion" /> (the cached topology is already up to date).
    /// </summary>
    /// <param name="cachedVersion">
    /// The version of the topology already cached locally, if any. When set, every seed is probed and the highest
    /// version wins within a grace period; when <c>null</c> (first fetch), the first successful answer wins and
    /// remaining answers are watched in the background for a newer version.
    /// </param>
    /// <param name="onHigherVersion">
    /// Callback invoked for answers whose version exceeds the returned best (background watcher), so a slow seed that
    /// reports a newer topology still triggers the replacement flow.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    internal async Task<GlobalTopology?> FetchTopologyAsync(
        long? cachedVersion,
        Func<GlobalTopology, Task>? onHigherVersion,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        Exception? lastError = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            IReadOnlyList<SrvTarget> targets;
            try
            {
                targets = ResolveSeeds(_globalHostname);
            }
#pragma warning disable CA1031 // SRV resolution failures are retried on the transient path; lastError is rethrown.
            catch (Exception ex)
            {
                lastError = ex;
                _log.GlobalSrvResolutionFailed(_globalHostname, attempt, maxRetries, ex.Message);
                if (attempt < maxRetries)
                {
                    await Task.Delay(BackoffWithJitter(attempt), cancellationToken).ConfigureAwait(false);
                }

                continue;
            }
#pragma warning restore CA1031

            if (targets.Count == 0)
            {
                // NXDOMAIN / empty answer is a deterministic "not provisioned" state: fail fast instead of falling
                // back to a direct-endpoint fetch (mirroring the Java SDK).
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError,
                    $"No SRV records found for global cluster endpoint '{_globalHostname}' ({SrvServicePrefix}{_globalHostname}).");
            }

            bool cachedRefresh = cachedVersion is not null;
            List<SrvTarget> candidates = PickCandidates(
                targets, cachedRefresh ? targets.Count : DefaultProbeCount);

            ProbeOutcome outcome = await ProbeCandidatesAsync(
                candidates, cachedRefresh, onHigherVersion, cancellationToken).ConfigureAwait(false);

            if (outcome.Best is not null)
            {
                if (!cachedRefresh || outcome.Best.Version > cachedVersion)
                {
                    return outcome.Best;
                }

                // Every reachable seed is at or behind the cached version: nothing newer on the server side.
                return null;
            }

            if (outcome.AuthenticationError is not null)
            {
                // Credentials are shared by every seed; retrying them cannot help.
                throw outcome.AuthenticationError;
            }

            if (outcome.ApiError is not null && outcome.LastError is null && outcome.ApiErrorsAgree)
            {
                // Every reachable seed returned the same API-level rejection.
                throw outcome.ApiError;
            }

            lastError = outcome.LastError ?? outcome.ApiError;
            _log.GlobalSeedsFailed(candidates.Count, _globalHostname, attempt, maxRetries,
                lastError is null ? "unknown error" : lastError.Message);
            if (attempt < maxRetries)
            {
                await Task.Delay(BackoffWithJitter(attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        string message = $"Failed to fetch global topology from seeds for '{_globalHostname}' after {maxRetries} attempts";
        throw lastError is null
            ? new MilvusException(MilvusErrorCode.UnexpectedError, message)
            : new MilvusException(
                MilvusErrorCode.UnexpectedError, message + "; last error: " + lastError.Message, lastError);
    }

    // Probes seeds concurrently and returns the best topology. With cachedRefresh, the first success starts a grace
    // deadline so an unavailable seed cannot stall recovery; without it the first successful answer wins. Remaining
    // answers are drained in the background and onHigherVersion triggers the replacement flow for each newer version.
    private async Task<ProbeOutcome> ProbeCandidatesAsync(
        IReadOnlyList<SrvTarget> candidates,
        bool cachedRefresh,
        Func<GlobalTopology, Task>? onHigherVersion,
        CancellationToken cancellationToken)
    {
        GlobalTopology? best = null;
        GlobalClusterApiException? apiError = null;
        GlobalClusterApiException? authenticationError = null;
        bool apiErrorsAgree = true;
        Exception? lastError = null;

        var results = new System.Collections.Concurrent.ConcurrentQueue<(SrvTarget Target, GlobalTopology? Topology, Exception? Error)>();
        var probes = new List<Task>();
        foreach (SrvTarget candidate in candidates)
        {
            SrvTarget target = candidate;
#pragma warning disable CA1031 // Each probe's failure is captured in the queue and classified by the caller.
            probes.Add(Task.Run(async () =>
            {
                try
                {
                    GlobalTopology topology = await ProbeSeedAsync(target, cancellationToken).ConfigureAwait(false);
                    results.Enqueue((target, topology, null));
                }
                catch (Exception ex)
                {
                    results.Enqueue((target, null, ex));
                }
            }, cancellationToken));
#pragma warning restore CA1031
        }

        int pending = probes.Count;
        DateTime? graceDeadline = null;

        // Consume results directly from the queue (mirroring the Java SDK's blocking take): pending counts the
        // results still to be consumed, so an early return can hand the exact remainder to the background watcher
        // without double-consuming anything. Every probe enqueues exactly one result (its HTTP request is bounded
        // by the HttpClient timeout), so the foreground poll terminates.
        while (pending > 0)
        {
            if (graceDeadline is { } deadline && DateTime.UtcNow >= deadline)
            {
                break;
            }

            if (!results.TryDequeue(out (SrvTarget Target, GlobalTopology? Topology, Exception? Error) result))
            {
                TimeSpan? remaining = graceDeadline is { } bound ? bound - DateTime.UtcNow : null;
                if (remaining is { } wait && wait <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay(
                    remaining is { } bounded ? (bounded < TimeSpan.FromMilliseconds(10) ? bounded : TimeSpan.FromMilliseconds(10)) : TimeSpan.FromMilliseconds(10),
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            pending--;

            if (result.Error is not null)
            {
                if (result.Error is GlobalClusterApiException apiException)
                {
                    if (apiError is null)
                    {
                        apiError = apiException;
                    }
                    else if (apiError.Code != apiException.Code)
                    {
                        apiErrorsAgree = false;
                    }

                    if (apiException.Code is 401 or 403 && authenticationError is null)
                    {
                        authenticationError = apiException;
                    }
                }
                else
                {
                    lastError = result.Error;
                    _log.GlobalProbeFailed(result.Target.ToString(), result.Error.Message);
                }

                continue;
            }

            if (best is null || result.Topology!.Version > best.Version)
            {
                best = result.Topology;
            }

            if (cachedRefresh)
            {
                // Once something usable arrives, bound the wait for lagging or unavailable seeds. Late answers
                // continue through the background watcher below.
                graceDeadline ??= DateTime.UtcNow + ProbeGracePeriod;
            }
            else
            {
                // First successful answer wins; the watcher keeps an eye on the rest.
                break;
            }
        }

        if (best is not null && onHigherVersion is not null && pending > 0)
        {
            _ = WatchRemainingAsync(results, pending, best.Version, onHigherVersion);
        }

        return new ProbeOutcome(best, apiError, authenticationError, apiErrorsAgree, lastError);
    }

    // Drains the answers still in flight and triggers replacement on a newer one (mirroring the Java SDK's
    // watchRemaining). Runs fire-and-forget after the first answer is handed over.
    private async Task WatchRemainingAsync(
        System.Collections.Concurrent.ConcurrentQueue<(SrvTarget Target, GlobalTopology? Topology, Exception? Error)> results,
        int remaining,
        long baselineVersion,
        Func<GlobalTopology, Task> onHigherVersion)
    {
        long bestVersion = baselineVersion;
        for (int i = 0; i < remaining; i++)
        {
            (SrvTarget Target, GlobalTopology? Topology, Exception? Error) result;
            while (!results.TryDequeue(out result))
            {
                await Task.Delay(10).ConfigureAwait(false);
            }

            if (result.Error is not null)
            {
                _log.GlobalProbeFailed(result.Target.ToString(), result.Error.Message);
                continue;
            }

            if (result.Topology!.Version > bestVersion)
            {
                bestVersion = result.Topology.Version;
#pragma warning disable CA1031 // The watcher is fire-and-forget; a failed replacement is logged and the rest drained.
                try
                {
                    await onHigherVersion(result.Topology).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.GlobalReplacementFailed(ex.Message);
                }
#pragma warning restore CA1031
            }
        }
    }

    // Fetches topology from a single seed. The global cluster is identified by the raw hostname, passed verbatim via
    // the ?endpoint= query parameter.
    private async Task<GlobalTopology> ProbeSeedAsync(SrvTarget seed, CancellationToken cancellationToken)
    {
        string url = BuildSeedTopologyUrl(seed, _globalHostname);
        using HttpResponseMessage response = await _http.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            // An authentication rejection (401/403) is deterministic and shared by every seed: fail fast instead of
            // retrying, mirroring the Java SDK's GlobalClusterApiException path. Any other HTTP error is a retryable
            // transport failure (a seed may be mid-switch), distinct from the API-level rejection ParseTopology
            // raises for a 200 body with a non-zero code.
            if ((int)response.StatusCode is 401 or 403)
            {
                string detail = await ReadBodyAsync(response.Content, cancellationToken).ConfigureAwait(false);
                throw new GlobalClusterApiException(
                    (int)response.StatusCode,
                    $"Global topology API authentication failed with HTTP status {(int)response.StatusCode}"
                    + (string.IsNullOrEmpty(detail) ? "" : $": {detail}"));
            }

            throw new HttpRequestException(
                $"Topology request failed with status {(int)response.StatusCode}: {await ReadBodyAsync(response.Content, cancellationToken).ConfigureAwait(false)}");
        }

        return ParseTopology(await ReadBodyAsync(response.Content, cancellationToken).ConfigureAwait(false));
    }

    private static GlobalTopology ParseTopology(string body)
    {
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;
        if (!root.TryGetProperty("code", out JsonElement codeElement))
        {
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, "Global topology response contains no code");
        }

        int code = codeElement.GetInt32();
        if (code != 0)
        {
            // A non-zero code is a deterministic server-side rejection (e.g. auth) that every seed shares.
            string message = root.TryGetProperty("message", out JsonElement msg) ? msg.GetString() ?? "" : "";
            throw new GlobalClusterApiException(code, $"Global topology API returned error code {code}: {message}");
        }

        if (!root.TryGetProperty("data", out JsonElement data)
            || !data.TryGetProperty("version", out JsonElement version)
            || !data.TryGetProperty("clusters", out JsonElement clusters))
        {
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, "Global topology response contains no data");
        }

        var infos = new List<ClusterInfo>();
        foreach (JsonElement cluster in clusters.EnumerateArray())
        {
            string clusterId = cluster.GetProperty("clusterId").GetString() ?? "";
            string endpoint = cluster.GetProperty("endpoint").GetString() ?? "";
            int capability = cluster.GetProperty("capability").GetInt32();
            infos.Add(new ClusterInfo(clusterId, endpoint, capability));
        }

        return new GlobalTopology(version.GetInt64(), infos);
    }

    // netstandard2.0/net462 lack the ReadAsStringAsync(CancellationToken) overload; the token is only used on
    // NET5+.
    private static async Task<string> ReadBodyAsync(
        HttpContent content, CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        return await content.ReadAsStringAsync().ConfigureAwait(false);
#endif
    }

    // Jitter is deliberately non-cryptographic (retry staggering only), so CA5394 does not apply.
#pragma warning disable CA5394 // Random is an insecure random number generator.
    private static TimeSpan BackoffWithJitter(int attempt)
    {
        // Exponential backoff 1s/2s/4s + 10% jitter, capped at 10s, matching the Java SDK's sleepBackoff
        // (BASE_BACKOFF_MS * (1 << (attempt - 1))).
        double factor = Math.Pow(2, attempt - 1);
        double delayMs = Math.Min(1000 * factor, 10000);
        return TimeSpan.FromMilliseconds(delayMs + delayMs * 0.1 * new Random().NextDouble());
    }
#pragma warning restore CA5394

    /// <summary>
    /// Starts the periodic topology refresh loop.
    /// </summary>
    internal void Start()
    {
        lock (_sync)
        {
            if (_refreshTimer is not null)
            {
                return;
            }

            _refreshTimer = new Timer(
                _ => { _ = RefreshAsync(); },
                null, RefreshInterval, RefreshInterval);
        }
    }

    /// <summary>
    /// Reacts to a connectivity failure (a gRPC <c>UNAVAILABLE</c> error or a server-side global-cluster routing
    /// error) by probing the topology for a primary change, debounced so a burst of retries does not hammer the
    /// global REST endpoint.
    /// </summary>
    internal void OnConnectivityFailure()
    {
        lock (_sync)
        {
            if (_disposed || DateTime.UtcNow - _lastUnavailableProbeUtc < UnavailableDebounce)
            {
                return;
            }

            _lastUnavailableProbeUtc = DateTime.UtcNow;
        }

        _ = RefreshAsync();
    }

    internal async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Serialize refreshes: a refresh already in flight (timer or connectivity-failure probe) skips this one,
        // matching the Java TopologyRefresher's refreshing flag.
        if (Interlocked.Exchange(ref _refreshing, 1) != 0)
        {
            return;
        }

        try
        {
            Func<GlobalTopology, Task>? callback = OnTopologyChange;
            long? cachedVersion = GetCachedVersion?.Invoke();
            GlobalTopology? topology = await FetchTopologyAsync(
                cachedVersion, callback, cancellationToken).ConfigureAwait(false);
            if (topology is null)
            {
                return;
            }

            if (callback is not null)
            {
#pragma warning disable CA1031 // A failed apply on the background path must not become an unobserved exception.
                try
                {
                    await callback(topology).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // A failed apply (e.g. a no-primary topology) must not surface as an unobserved exception out of
                    // the timer / probe path; keep the current topology and log instead (Java refresh() wraps
                    // the same way).
                    _log.GlobalTopologyApplyFailed(ex.Message);
                }
#pragma warning restore CA1031
            }
        }
        catch (Exception ex) when (ex is MilvusException or GlobalClusterApiException)
        {
            // A fetch failure (SRV not provisioned, auth, API rejection, exhausted retries) keeps the cached
            // topology on the background path; the first connect surfaces it via EnsureGlobalPrimaryAsync.
            _log.GlobalTopologyParseFailed(ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _refreshing, 0);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _http.Dispose();
        }
    }

    private sealed class ProbeOutcome
    {
        internal ProbeOutcome(
            GlobalTopology? best, GlobalClusterApiException? apiError, GlobalClusterApiException? authenticationError,
            bool apiErrorsAgree, Exception? lastError)
        {
            Best = best;
            ApiError = apiError;
            AuthenticationError = authenticationError;
            ApiErrorsAgree = apiErrorsAgree;
            LastError = lastError;
        }

        internal GlobalTopology? Best { get; }
        internal GlobalClusterApiException? ApiError { get; }
        internal GlobalClusterApiException? AuthenticationError { get; }
        internal bool ApiErrorsAgree { get; }
        internal Exception? LastError { get; }
    }
}
