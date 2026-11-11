#if NET5_0_OR_GREATER
using System.Net.Http;
#endif
using System.Runtime.CompilerServices;
using System.Text;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

/// <summary>
/// Milvus gRPC client (V2).
/// </summary>
/// <remarks>
/// All operations take a request object (DTO) and return a typed response, following the same pattern as the
/// Java/C++/Rust Milvus V2 SDKs.
///
/// <b>Thread safety.</b> Once the client is constructed, RPC methods may be called concurrently on the same
/// instance, subject to the following restrictions:
/// <list type="bullet">
/// <item>Concurrent DML and DQL calls are supported when their input and output objects (requests, iterators,
/// field data) are not shared between the calls.</item>
/// <item>DDL operations that change a collection's identity, schema, or aliases (create/drop/rename/alter,
/// add/drop field, alias operations) must be serialized with DML and DQL calls, as the server may not see a
/// consistent schema otherwise.</item>
/// <item>Connection-lifecycle methods — the constructor, <see cref="ConnectAsync" />, <see cref="UseDatabaseAsync" />
/// and <see cref="Dispose" /> — must be serialized with all other calls.</item>
/// <item>Do not dispose the client while a query/search iterator obtained from it is still being consumed.</item>
/// </list>
/// </remarks>
public sealed partial class MilvusClientV2 : IDisposable
{
    // Marker the global-cluster topology manager / retry path looks for in a server error reason to detect a
    // global-cluster routing error (the current primary cannot serve the request; probe the topology for a primary
    // change and retry), mirroring the Java SDK's RpcUtils.handleGlobalRoutingError.
    private const string GlobalRoutingErrorMarker = "STREAMING_CODE_REPLICATE_VIOLATION";

    /// <summary>
    /// Creates a new <see cref="MilvusClientV2" />, connecting to the given Milvus instance.
    /// </summary>
    /// <param name="config">The connection parameters, mirroring the <c>ConnectParam</c> of the C++ SDK and the
    /// <c>ConnectConfig</c> of the Java SDK.</param>
    public MilvusClientV2(ConnectConfig config)
        : this(config, globalClusterHttpHandler: null, globalClusterSrvResolver: null)
    {
    }

    // Test seams: the handler used for global-cluster topology REST calls, and the SRV resolver used to discover
    // the ha-manager seeds. The public constructor uses the default platform handler / DnsClient; tests inject
    // canned-response handler and seed list per instance so the topology fetch does not depend on real DNS.
    internal MilvusClientV2(
        ConnectConfig config,
        System.Net.Http.HttpMessageHandler? globalClusterHttpHandler,
        Func<string, IReadOnlyList<SrvTarget>>? globalClusterSrvResolver)
    {
        Verify.NotNull(config);

        // Validate auth config before allocating the channel, so a config error cannot leak the channel's
        // sockets/handler without disposal.
        if (config.ApiKey is not null && config.Username is not null)
        {
            throw new ArgumentException(
                "ApiKey and Username are mutually exclusive authentication modes; specify only one.",
                nameof(config));
        }

        _endpoint = TryGetEndpoint(config.Uri);
        _connectConfig = config;

        // A global-cluster endpoint (hostname contains "global-cluster") fronts a Zilliz Cloud deployment whose
        // member topology must be fetched from the global REST endpoint before the physical primary channel can be
        // built; the channel is created lazily on connect. A regular endpoint builds its channel eagerly.
        _globalCluster = GlobalClusterManager.TryCreate(config, globalClusterHttpHandler);
        if (_globalCluster is not null && globalClusterSrvResolver is not null)
        {
            _globalCluster.ResolveSrv = globalClusterSrvResolver;
        }

        if (_globalCluster is null)
        {
            CreateChannelAndClients(config.Uri);
        }

        _telemetry = new ClientTelemetryManager(
            config.Telemetry ?? new TelemetryConfig(),
            _telemetryGrpcClient,
            () =>
            {
                // Read the (now writable) username under the connect lock so a concurrent
                // ResetConnectionAfterPasswordChange cannot interleave a torn read.
                lock (_connectLock)
                {
                    return _username ?? "";
                }
            },
            () => ResolveDatabaseName(null),
            () => typeof(MilvusClientV2).Assembly.GetName().Version?.ToString() ?? "unknown",
            CreateCallOptions,
            () => _endpoint);

        string? authorization = config.Username is null
            ? config.ApiKey
            : $"{config.Username}:{config.Password}";

        var metadata = new Metadata();

        if (authorization is not null)
        {
            _authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization));
            metadata.Add("authorization", _authorizationHeader);
        }

        // Mirror the _database normalization below: a null/whitespace Database must not leak a bogus dbname
        // header while every ts/schema cache key resolves to "default".
        if (!string.IsNullOrWhiteSpace(config.Database))
        {
            metadata.Add("dbname", config.Database!);
        }

        if (metadata.Count > 0)
        {
            _callOptions = _callOptions.WithHeaders(metadata);
        }

        if (config.ConnectTimeout is not null)
        {
            _connectTimeout = config.ConnectTimeout.Value;
        }

        _log = config.LoggerFactory?.CreateLogger("Milvus.Client.V2") ?? NullLogger.Instance;

        _username = config.Username ?? "";
        _password = config.Password ?? "";
        _retryConfig = config.Retry ?? new RetryConfig();
        _retryConfig.Validate();
        _database = string.IsNullOrWhiteSpace(config.Database) ? "default" : config.Database!;
    }

    /// <summary>
    /// Connects to the Milvus server, registering the client info (SDK type/version, user, host, local time).
    /// Recommended before using other APIs so connection/authentication failures surface up front.
    /// If not called explicitly, the first API call connects lazily.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connectTask is not null)
            {
                // The shared connect task was created without any caller's token; race this caller's own
                // token against it so cancelling one caller does not tear down the shared connect, but does
                // unblock that caller promptly.
                await WaitWithCallerTokenAsync(_connectTask, cancellationToken).ConfigureAwait(false);
                return;
            }

            // The shared in-flight connect task is created without the winning caller's token: a cancel from any
            // single caller must not tear down the connect that all concurrent callers are waiting on.
            _connectTask = ConnectRpcAsync(CancellationToken.None);
            try
            {
                await _connectTask.ConfigureAwait(false);
            }
            catch
            {
                // Allow retrying ConnectAsync after a failure.
                _connectTask = null;
                throw;
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    internal async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_connectTask is not null)
        {
            await WaitWithCallerTokenAsync(_connectTask, cancellationToken).ConfigureAwait(false);
            return;
        }

        await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    // Task.WaitAsync is unavailable on netstandard2.0/net462; race the task against a cancel-only delay so the
    // caller's token still surfaces a cancellation promptly without tearing down the shared connect task.
    private static async Task WaitWithCallerTokenAsync(Task task, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            await task.ConfigureAwait(false);
            return;
        }

        TaskCompletionSource<bool> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
        {
            Task completed = await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false);
            if (completed == cancelled.Task)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        await task.ConfigureAwait(false);
    }

    private async Task ConnectRpcAsync(CancellationToken cancellationToken)
    {
        // A global-cluster endpoint defers channel creation until connect: fetch the topology, connect to the
        // current primary, and arm the topology refresher so a later primary change fails the client over.
        if (_globalCluster is not null)
        {
            await EnsureGlobalPrimaryAsync(cancellationToken).ConfigureAwait(false);
        }

        // After the global-primary resolution (or for a regular endpoint, from construction) the channel exists.
        GrpcChannel channel = _grpcChannel!;
        var request = new Grpc.ConnectRequest
        {
            ClientInfo = new Grpc.ClientInfo
            {
                SdkType = "CSharp",
                SdkVersion = typeof(MilvusClientV2).Assembly.GetName().Version?.ToString() ?? "unknown",
                User = _username,
                Host = TryGetUriHost(channel.Target),
                LocalTime = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
            }
        };

        try
        {
            Grpc.ConnectResponse response = await GrpcClient.ConnectAsync(
                request, CreateCallOptions(cancellationToken)).ConfigureAwait(false);

            var code = (MilvusErrorCode)response.Status.Code;

            if (code != MilvusErrorCode.Success)
            {
                throw new MilvusException(code, response.Status.Reason);
            }

            // Start the client-side telemetry heartbeat once connected.
            _telemetry.Start();
        }
        catch (RpcException ex)
        {
            // Transport/auth failures surface here (explicit ConnectAsync or lazy first call).
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, $"Failed to connect to Milvus: {ex.StatusCode} {ex.Status.Detail}",
                ex.StatusCode, ex);
        }
    }

    // Resolves the physical primary channel for a global-cluster client: fetches the topology, connects to the
    // current primary, and arms the refresh / failover hooks.
    private async Task EnsureGlobalPrimaryAsync(CancellationToken cancellationToken)
    {
        GlobalClusterManager manager = _globalCluster!;

        // Arm the failover hooks first so the first-fetch watcher (a slow seed reporting a newer topology) can
        // already fail the client over.
        manager.OnTopologyChange = OnGlobalTopologyChange;
        manager.GetCachedVersion = () =>
        {
            // Interlocked.Read: a torn 64-bit read on the 32-bit targets could otherwise gate the stale-discard on a
            // garbage version.
            long version = Interlocked.Read(ref _appliedTopologyVersion);
            return version < 0 ? (long?)null : version;
        };

        // First connection: fetch the topology (no cached version) and connect to its primary.
        if (_grpcChannel is null)
        {
            GlobalTopology? topology;
            try
            {
                topology = await manager.FetchTopologyAsync(
                    cachedVersion: null, onHigherVersion: OnGlobalTopologyChange, cancellationToken).ConfigureAwait(false);
            }
            catch (GlobalClusterApiException ex)
            {
                // The topology API exception type is internal; surface a MilvusException at the public boundary
                // (Java surfaces its public GlobalClusterApiException directly).
                throw new MilvusException(MilvusErrorCode.UnexpectedError, ex.Message, ex);
            }

            if (topology is null)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, "Failed to fetch global cluster topology.");
                }

            // Validate the first topology has a primary before applying: OnGlobalTopologyChange treats a
            // no-primary topology as a no-op, but the first connect must fail loudly.
            try
            {
                _ = topology.Primary;
            }
            catch (InvalidOperationException ex)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, "No primary (writable) cluster found in global topology.", ex);
            }

            // Apply through the same version compare-and-set the refresh paths use, so a concurrent initial-fetch
            // watcher that already installed a newer topology cannot be overwritten by this older first answer
            // (mirroring the Java GlobalStub, where every update funnels through the CAS under the topology lock).
            await OnGlobalTopologyChange(topology).ConfigureAwait(false);
            if (_grpcChannel is null)
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, "Failed to connect to a global cluster primary.");
            }
        }

        // Start the periodic refresher (idempotent): a newer topology whose primary differs rebuilds the channel; a
        // gRPC UNAVAILABLE error probes the topology for a primary change.
        manager.Start();
    }

    // Internal test hook: forces a topology refresh so a primary change can be exercised deterministically.
    internal async Task RefreshGlobalTopologyAsync(CancellationToken cancellationToken = default)
    {
        if (_globalCluster is null)
        {
            return;
        }

        await EnsureGlobalPrimaryAsync(cancellationToken).ConfigureAwait(false);
        await _globalCluster.RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    // Called by the GlobalClusterManager after a topology refresh. A topology no newer than the last applied one is
    // stale (an old answer landing after a newer one, e.g. timer vs UNAVAILABLE probe) and is discarded so the
    // client is not flipped back to a previous primary.
    //
    // Every write to the shared topology state funnels through this compare-and-set under _connectLock, so no
    // update path (initial-fetch watcher, periodic refresh, connectivity-failure probe) can roll the version back
    // however the threads interleave, mirroring the Java GlobalStub.onTopologyChange.
    private Task OnGlobalTopologyChange(GlobalTopology newTopology)
    {
        lock (_connectLock)
        {
            // The client is being disposed; a topology refresh still in flight must not fail over onto a channel
            // that was (or is about to be) disposed.
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (newTopology.Version <= _appliedTopologyVersion)
            {
                _log.GlobalTopologyStale(newTopology.Version, _appliedTopologyVersion);
                return Task.CompletedTask;
            }

            ClusterInfo newPrimary;
            try
            {
                newPrimary = newTopology.Primary;
            }
            catch (InvalidOperationException)
            {
                // A topology with no writable cluster cannot be applied; keep the current primary.
                _log.GlobalTopologyNoPrimary(newTopology.Version);
                return Task.CompletedTask;
            }

            string newEndpoint = newPrimary.Endpoint;
            string newConnectionUri = NormalizePrimaryUri(_connectConfig, newEndpoint);
            string? currentConnectionUri = _primaryEndpoint is null
                ? null
                : NormalizePrimaryUri(_connectConfig, _primaryEndpoint);

            if (newConnectionUri.Equals(currentConnectionUri, StringComparison.Ordinal))
            {
                // The primary endpoint is unchanged; only the (newer) version is recorded.
                _appliedTopologyVersion = newTopology.Version;
                return Task.CompletedTask;
            }

            _log.GlobalPrimaryChanged(_primaryEndpoint ?? "", newEndpoint);
            ApplyPrimary(newEndpoint);
            _appliedTopologyVersion = newTopology.Version;
            return Task.CompletedTask;
        }
    }

    // Rebuilds the physical channel and both gRPC clients to the given primary endpoint (host:port). The cache key
    // endpoint (_endpoint) is left pointing at the logical global endpoint, matching the Java SDK, so schema/ts
    // caches recorded before a failover remain reachable after it.
    private void ApplyPrimary(string primaryEndpoint)
    {
        CreateChannelAndClients(NormalizePrimaryUri(_connectConfig, primaryEndpoint));
        _primaryEndpoint = primaryEndpoint;
    }

    /// <summary>
    /// The current physical primary endpoint (host:port) this global-cluster client is connected to, or <c>null</c>
    /// for a regular client / before the first connect.
    /// </summary>
    internal string? PrimaryEndpoint => _primaryEndpoint;

    // Normalizes a primary endpoint to "scheme://host:port" using the same rules as the Java SDK's
    // GlobalStub.normalizePrimaryUri: a scheme-less endpoint inherits the original config's https/http scheme, and
    // an absent port becomes 443 (https) or 19530 (http). Used both to build the replacement channel and to decide
    // whether a newer topology actually moves the primary.
    private static string NormalizePrimaryUri(ConnectConfig config, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new MilvusException(MilvusErrorCode.UnexpectedError, "Primary endpoint is required");
        }

        string value = endpoint.Trim();
        bool isHttps = value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        bool isHttp = value.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !isHttp)
        {
#pragma warning disable CA2249 // netstandard2.0/net462 have no string.Contains(string, StringComparison).
            if (value.IndexOf("://", StringComparison.Ordinal) >= 0)
#pragma warning restore CA2249
            {
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, "Unsupported primary endpoint scheme: " + endpoint);
            }

            // Global endpoints are https (Zilliz Cloud); a scheme-less primary inherits the original config's scheme,
            // matching the Java SDK's isSecure().
            isHttps = config.Uri.StartsWith("https", StringComparison.OrdinalIgnoreCase);
            value = (isHttps ? "https://" : "http://") + value;
        }

        try
        {
            Uri parsed = new(value);
            string host = parsed.Host;
            if (string.IsNullOrEmpty(host))
            {
                throw new MilvusException(MilvusErrorCode.UnexpectedError, "Invalid primary endpoint: " + endpoint);
            }

            int port = parsed.IsDefaultPort
                ? (isHttps ? 443 : 19530)
                : parsed.Port;
            return $"{parsed.Scheme}://{host}:{port}";
        }
        catch (UriFormatException ex)
        {
            throw new MilvusException(MilvusErrorCode.UnexpectedError, "Invalid primary endpoint: " + endpoint, ex);
        }
    }

    // A global-cluster routing error is retried only for a global-cluster client (mirroring the Java SDK, where
    // handleGlobalRoutingError is a no-op when no global refresh trigger is installed); regular clients surface it
    // immediately like any other server error.
    private bool IsGlobalClusterRoutingErrorRetryable(Exception exception)
        => _globalCluster is not null
           && exception is MilvusException
           && IsGlobalRoutingError(exception);

    // Invoked by the retry loop on each retryable attempt, mirroring the Java SDK's handleGlobalConnectionError /
    // handleGlobalRoutingError: a gRPC UNAVAILABLE or a server-side global-cluster routing error probes the
    // topology for a primary change (debounced inside the manager), so the next call already targets the new
    // primary.
    private void OnGlobalClusterAttemptFailed(Exception exception)
    {
        if (_globalCluster is null)
        {
            return;
        }

        if (exception is RpcException { StatusCode: StatusCode.Unavailable } || IsGlobalRoutingError(exception))
        {
            _globalCluster.OnConnectivityFailure();
        }
    }

    // netstandard2.0/net462 have no string.Contains(string, StringComparison); IndexOf is the shared spelling.
    private static bool IsGlobalRoutingError(Exception exception)
    {
#pragma warning disable CA2249 // Consider using string.Contains for readability
        return exception.Message.IndexOf(GlobalRoutingErrorMarker, StringComparison.Ordinal) >= 0;
#pragma warning restore CA2249
    }

    private static string TryGetUriHost(string uri)
    {
        try
        {
            string normalized = uri.IndexOf("://", StringComparison.Ordinal) < 0 ? $"http://{uri}" : uri;
            return new Uri(normalized).Host;
        }
        catch (UriFormatException)
        {
            return "";
        }
    }

    private static string TryGetEndpoint(string uri)
    {
        try
        {
            string normalized = uri.IndexOf("://", StringComparison.Ordinal) < 0 ? $"http://{uri}" : uri;
            Uri parsed = new(normalized);
            // Uri.Port returns the scheme default (80/443) when the caller omits the port; normalize to
            // Milvus's default (19530) so equivalent URIs produce the same cache key.
            return $"{parsed.Host}:{(parsed.IsDefaultPort ? 19530 : parsed.Port)}";
        }
        catch (UriFormatException)
        {
            return "";
        }
    }

    /// <summary>
    /// Base address of the Milvus server.
    /// </summary>
    public string Address => _grpcChannel?.Target ?? _endpoint;

    /// <summary>
    /// Checks the health of the Milvus server.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<HealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.CheckHealthResponse response = await InvokeAsync(
                GrpcClient.CheckHealthAsync, new Grpc.CheckHealthRequest(), static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsHealthy)
        {
            _log.HealthCheckFailed(response.Reasons);
        }

        return new HealthState(response.IsHealthy, response.Status.Reason,
            (MilvusErrorCode)response.Status.Code,
            response.Reasons.ToList(),
            response.QuotaStates.Select(q => (QuotaState)(int)q).ToList());
    }

    /// <summary>
    /// Checks the health of the Milvus server (alias of <see cref="HealthAsync" />, matching the design doc §4.12
    /// API name).
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task<HealthState> CheckHealthAsync(CancellationToken cancellationToken = default)
        => HealthAsync(cancellationToken);

    /// <summary>
    /// Gets the version of this SDK, matching the C++ SDK's <c>GetSDKVersion</c>. Returns the assembly version
    /// of <c>Milvus.Client.V2</c>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:MarkMembersAsStatic",
        Justification = "Instance member for API parity with the C++ GetSDKVersion.")]
    public string GetSdkVersion()
        => typeof(MilvusClientV2).Assembly.GetName().Version?.ToString() ?? "unknown";

    /// <summary>
    /// Gets the client-side telemetry manager that records operation metrics, reports them to the server over a
    /// heartbeat loop, and handles server-pushed commands. Mirrors the Java SDK's <c>getTelemetry</c> / the C++
    /// SDK's <c>GetTelemetry</c>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1024:UsePropertiesWhereAppropriate",
        Justification = "Method for API parity with java getTelemetry / cpp GetTelemetry.")]
    public ClientTelemetryManager GetTelemetry()
        => _telemetry;

    /// <summary>
    /// Gets the Milvus server version.
    /// </summary>
    /// <param name="request">The request specifying whether to return detailed version information.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GetServerVersionResp> GetServerVersionAsync(
        GetServerVersionReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        if (request.Detail)
        {
            Grpc.ConnectResponse connectResponse = await InvokeAsync(
                GrpcClient.ConnectAsync, new Grpc.ConnectRequest(), static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

            return GetServerVersionResp.FromGrpc(connectResponse);
        }

        Grpc.GetVersionResponse response = await InvokeAsync(
            GrpcClient.GetVersionAsync, new Grpc.GetVersionRequest(), static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return GetServerVersionResp.FromGrpc(response);
    }

    /// <inheritdoc />
    public override string ToString() => $"{{{nameof(MilvusClientV2)}:{Address}}}";

    /// <inheritdoc />
    public void Dispose()
    {
        _telemetry.Dispose();
        _globalCluster?.Dispose();
        lock (_connectLock)
        {
            // Fence further failovers: a topology refresh already in flight must not swap in a channel after the
            // current ones are disposed (mirroring the Java GlobalStub's closed flag).
            _disposed = true;

            _grpcChannel?.Dispose();
            foreach (GrpcChannel retired in _retiredChannels)
            {
                retired.Dispose();
            }

            _retiredChannels.Clear();
        }

        _connectLock.Dispose();
    }

    private static GrpcChannelOptions CreateDefaultChannelOptions(ConnectConfig config)
    {
        var options = new GrpcChannelOptions { LoggerFactory = config.LoggerFactory };

#if NET5_0_OR_GREATER
        // Keepalive pings keep the channel healthy across idle gaps and through load-balancer idle timeouts,
        // mirroring the Java SDK's keepAliveTimeMs/keepAliveTimeoutMs/keepAliveWithoutCalls.
        options.HttpHandler = new SocketsHttpHandler
        {
            KeepAlivePingDelay = config.KeepAliveTime ?? TimeSpan.FromSeconds(10),
            KeepAlivePingTimeout = config.KeepAliveTimeout ?? TimeSpan.FromSeconds(5),
            KeepAlivePingPolicy = config.KeepAliveWithoutCalls is false
                ? HttpKeepAlivePingPolicy.WithActiveRequests
                : HttpKeepAlivePingPolicy.Always
        };
#endif

        return options;
    }

    private static GrpcChannel CreateDefaultChannel(ConnectConfig config)
    {
        Verify.NotNull(config);
        Verify.NotNullOrWhiteSpace(config.Uri);

        string uri = config.Uri;
        if (uri.IndexOf("://", StringComparison.Ordinal) < 0)
        {
            uri = $"http://{uri}";
        }

        return GrpcChannel.ForAddress(uri, config.ChannelOptions ?? CreateDefaultChannelOptions(config));
    }

    // Builds the physical channel and the two gRPC clients for the given endpoint. Used for the initial (regular)
    // connection and for every global-cluster primary (re)connect.
    //
    // The swap is serialized under _connectLock so two overlapping refreshes (5-min timer + UNAVAILABLE probe)
    // cannot interleave and orphan a channel. The old channel is NOT disposed here: in-flight calls may still hold a
    // reference to the old MilvusServiceClient and would throw ObjectDisposedException mid-RPC. It is retired and
    // disposed by Dispose() once the client (and all calls) are done.
    private void CreateChannelAndClients(string uri)
    {
        lock (_connectLock)
        {
            GrpcChannel newChannel = CreateDefaultChannel(
                new ConnectConfig
                {
                    Uri = uri,
                    LoggerFactory = _connectConfig.LoggerFactory,
                    ChannelOptions = _connectConfig.ChannelOptions,
                    KeepAliveTime = _connectConfig.KeepAliveTime,
                    KeepAliveTimeout = _connectConfig.KeepAliveTimeout,
                    KeepAliveWithoutCalls = _connectConfig.KeepAliveWithoutCalls
                });

            // Swap the channel and both clients transactionally: if anything after the channel creation fails the
            // old channel/stubs are restored (mirroring the Java GlobalStub.onTopologyChange rollback), so a failed
            // failover never leaves the client half-moved.
            GrpcChannel? previousChannel = _grpcChannel;
            Grpc.MilvusService.MilvusServiceClient? previousClient = _grpcClient;
            Grpc.ClientTelemetryService.ClientTelemetryServiceClient? previousTelemetry = _telemetryGrpcClient;
            try
            {
                _grpcChannel = newChannel;
                _grpcClient = new Grpc.MilvusService.MilvusServiceClient(newChannel);
                _telemetryGrpcClient = new Grpc.ClientTelemetryService.ClientTelemetryServiceClient(newChannel);
                _telemetry?.Retarget(_telemetryGrpcClient);
            }
            catch (Exception)
            {
                _grpcChannel = previousChannel;
                _grpcClient = previousClient;
                _telemetryGrpcClient = previousTelemetry;
                newChannel.Dispose();
                throw;
            }

            // Retire the old channel only after the swap succeeded: in-flight calls may still hold a reference to
            // the old MilvusServiceClient and would throw ObjectDisposedException mid-RPC. It is retired and
            // disposed by Dispose() once the client (and all calls) are done.
            if (previousChannel is not null)
            {
                _retiredChannels.Add(previousChannel);
            }
        }
    }

    private readonly ILogger _log;

    // Volatile: the failover writer (CreateChannelAndClients) swaps this under the connect lock while the connect
    // path and Address read it without the lock; volatile keeps every read a consistent old-or-new reference.
    private volatile GrpcChannel? _grpcChannel;

    // Channels retired by global-cluster failover, kept alive (not disposed) until client Dispose so a call that
    // already captured the old MilvusServiceClient cannot hit ObjectDisposedException mid-RPC. Guarded by
    // _connectLock, which also serializes the channel swap.
    private readonly List<GrpcChannel> _retiredChannels = new();

    private string _username;
    private string _password;
    private readonly RetryConfig _retryConfig;
    private readonly string _endpoint;
    private readonly TimeSpan? _connectTimeout;
    private string _database;
    private string? _authorizationHeader;

    // Global-cluster (Zilliz Cloud) topology manager; null for a regular endpoint. Owns topology discovery,
    // periodic refresh and the UNAVAILABLE-triggered probe; on a primary change it rebuilds the gRPC channel.
    private readonly GlobalClusterManager? _globalCluster;

    // Original connect config, retained for global-cluster failover: a replacement primary channel is built with
    // the same credentials/options but the primary's gRPC endpoint.
    private readonly ConnectConfig _connectConfig;

    // The version of the topology currently applied; refreshes whose version is not newer are discarded so an old
    // answer landing after a newer one cannot flip the client back to a previous primary. Written under the
    // connect lock and read elsewhere through Volatile.Read.
    private long _appliedTopologyVersion = -1;

    // Set under the connect lock by Dispose; a topology refresh still in flight must not fail over after the
    // channels are disposed.
    private bool _disposed;

    // The physical gRPC clients, replaced on global-cluster failover. Read through the public/internal properties
    // below so existing call sites keep working across a channel swap.
    private volatile Grpc.MilvusService.MilvusServiceClient _grpcClient = null!;
    private volatile Grpc.ClientTelemetryService.ClientTelemetryServiceClient _telemetryGrpcClient = null!;

    // The current physical primary endpoint (host:port), tracked explicitly so failover is observable independently
    // of the channel's Target string (which may normalize differently). Null until the first channel exists.
    private volatile string? _primaryEndpoint;

    // Depth of the enclosing logical telemetry operation (e.g. a schema-mismatch retry that re-enters the
    // facade). InvokeAsync records only at depth 0 so a logical Insert/Upsert is recorded exactly once, while
    // the facade wrapper records the final outcome (Java logicalOperationDepth, C++ InvokeWithTelemetry).
    private static readonly AsyncLocal<int> LogicalOperationDepth = new();

    private CallOptions _callOptions;

    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private Task? _connectTask;

    // The currently selected database, read atomically. UseDatabaseAsync updates it together with the
    // dbname metadata header under _connectLock; readers must go through this property (and ReadCallOptions)
    // so a concurrent database switch cannot hand an RPC a header set inconsistent with CurrentDatabase.
    // Note this only keeps the RPC header paired with the database used to derive the session guarantee
    // timestamp when that timestamp is computed and the call options are snapshotted back-to-back; a
    // UseDatabaseAsync landing in between can still pair a database-B header with a guarantee timestamp read
    // from database A's CollectionTsCache. That window is narrow (no await between the reads on the DQL
    // paths) and at worst produces an eventually-consistent timestamp, which the server tolerates.
    internal string CurrentDatabase => Volatile.Read(ref _database);

    // Resolves the effective database for an operation: a non-empty request-level DatabaseName overrides the
    // client's currently selected database, mirroring Java's actualDbName / C++'s "use default database if
    // empty" contract. Callers use this everywhere the database feeds a proto db_name, a schema/ts cache key,
    // or a Session guarantee timestamp, so a request-level override is honored consistently.
    internal string ResolveDatabaseName(string? requestDatabaseName)
        => string.IsNullOrEmpty(requestDatabaseName) ? CurrentDatabase : requestDatabaseName!;

    internal Grpc.MilvusService.MilvusServiceClient GrpcClient => _grpcClient;

    internal Grpc.ClientTelemetryService.ClientTelemetryServiceClient TelemetryGrpcClient => _telemetryGrpcClient;

    private readonly ClientTelemetryManager _telemetry;

    // Server-streaming calls that can outlive the connect timeout (e.g. an unbounded DumpMessages stream with
    // EndTimetick=0) must not inherit the per-call ConnectTimeout deadline, which is intended for unary calls.
    internal CallOptions CallOptionsForStreaming(CancellationToken cancellationToken)
        => ReadCallOptions().WithCancellationToken(cancellationToken);

    // Applies a fresh ConnectTimeout deadline on every call, rather than a fixed absolute deadline captured once
    // at construction time (which would expire and break every later call on a long-lived client).
    private CallOptions CreateCallOptions(CancellationToken cancellationToken)
    {
        CallOptions options = ReadCallOptions().WithCancellationToken(cancellationToken);
        if (_connectTimeout is { } timeout)
        {
            options = options.WithDeadline(DateTime.UtcNow.Add(timeout));
        }

        return options;
    }

    // Snapshots the shared _callOptions under the connect lock so a concurrent UseDatabaseAsync cannot
    // interleave mid-swap and hand an RPC a header set inconsistent with CurrentDatabase.
    private CallOptions ReadCallOptions()
    {
        lock (_connectLock)
        {
            return _callOptions;
        }
    }

    // After the current user's password is changed, rebuild the authorization header and call options so
    // subsequent RPCs authenticate with the new credentials (matching the Java SDK's resetConnection and
    // the C++ SDK's reconnect-with-new-credentials). No-op for ApiKey-based or a different user.
    internal void RefreshAuthorizationAfterPasswordChange(string changedUserName, string newPassword)
    {
        lock (_connectLock)
        {
            // The username check runs under the connect lock so a concurrent ResetConnectionAfterPasswordChange
            // cannot interleave a stale read.
            if (_username.Length == 0 || !string.Equals(_username, changedUserName, StringComparison.Ordinal)
                || newPassword is null)
            {
                return;
            }

            _password = newPassword;

            var metadata = new Metadata();
            string authorization = $"{_username}:{_password}";
            _authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization));
            metadata.Add("authorization", _authorizationHeader);

            // Preserve the selected database header across the password refresh.
            if (CurrentDatabase is { Length: > 0 } database)
            {
                metadata.Add("dbname", database);
            }

            _callOptions = _callOptions.WithHeaders(metadata);
        }
    }

    // Explicitly resets the connection as the given user with the new password, authenticating subsequent
    // RPCs as that user and forcing a reconnect so the server re-registers the client.
    internal void ResetConnectionAfterPasswordChange(string userName, string newPassword)
    {
        lock (_connectLock)
        {
            _username = userName;
            _password = newPassword;

            var metadata = new Metadata();
            string authorization = $"{_username}:{_password}";
            _authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization));
            metadata.Add("authorization", _authorizationHeader);

            // Preserve the selected database header across the password reset.
            if (CurrentDatabase is { Length: > 0 } database)
            {
                metadata.Add("dbname", database);
            }

            _callOptions = _callOptions.WithHeaders(metadata);

            // Drop the shared connect task so the next API call reconnects and registers the client info as
            // the updated user.
            _connectTask = null;
        }
    }

    internal Task<Grpc.Status> InvokeAsync<TRequest>(
        Func<TRequest, CallOptions, AsyncUnaryCall<Grpc.Status>> func,
        TRequest request,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = "")
        where TRequest : class
        => InvokeAsync(func, request, static r => r, cancellationToken, callerName);

    internal async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        Func<TRequest, CallOptions, AsyncUnaryCall<TResponse>> func,
        TRequest request,
        Func<TResponse, Grpc.Status> getStatus,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = "")
        where TRequest : class
    {
        // RBAC credential requests carry password material that must not be written to the debug log. The RBAC
        // facades pass the generated gRPC message (CreateCredentialRequest / UpdateCredentialRequest), not the
        // request DTO, so redact on the wire types.
        _log.OperationInvoked(
            callerName,
            request is Grpc.CreateCredentialRequest or Grpc.UpdateCredentialRequest
                ? new { credential = "redacted" }
                : request);

        // Record the logical operation (Insert/Delete/Upsert/Search/HybridSearch/Query/RunAnalyzer) with the
        // whole retried call's latency and outcome, mirroring the Java/C++/Rust telemetry managers. When an
        // enclosing logical operation is active (LogicalOperationDepth > 0, e.g. a schema-mismatch retry that
        // re-enters the facade), skip per-attempt recording; the facade wrapper records the final outcome once.
        string? telemetryOperation = LogicalOperationDepth.Value == 0 ? ToTelemetryOperation(callerName) : null;
        string? telemetryCollection = telemetryOperation is null ? null : TryGetTelemetryCollection(request);
        System.Diagnostics.Stopwatch? telemetryStopwatch = telemetryOperation is null ? null : System.Diagnostics.Stopwatch.StartNew();

        try
        {
            try
            {
                TResponse response = await RetryPolicy.ExecuteAsync(
                    async innerCt =>
                    {
                        TResponse innerResponse = await func(request, CreateCallOptions(innerCt)).ConfigureAwait(false);
                        Grpc.Status status = getStatus(innerResponse);
                        var code = (MilvusErrorCode)status.Code;

                        // Pre-2.4 servers populate only the legacy error_code field (code stays 0); fall back to it
                        // so their non-success responses are not silently swallowed as Success.
#pragma warning disable CS0612 // ErrorCode is [deprecated] but required for pre-2.4 server compatibility.
                        if (code == MilvusErrorCode.Success && status.ErrorCode != Grpc.ErrorCode.Success)
                        {
                            code = (MilvusErrorCode)(int)status.ErrorCode;
                        }
#pragma warning restore CS0612

                        if (code != MilvusErrorCode.Success)
                        {
                            _log.OperationFailed(callerName, code, status.Reason);

                            throw new MilvusException(code, status.Reason);
                        }

                        return innerResponse;
                    },
                    _retryConfig,
                    cancellationToken,
                    IsGlobalClusterRoutingErrorRetryable,
                    OnGlobalClusterAttemptFailed).ConfigureAwait(false);

                if (telemetryStopwatch is not null)
                {
#pragma warning disable CA1031 // Telemetry must never throw into the caller; the RPC result is authoritative.
                    try
                    {
                        _telemetry.RecordOperation(telemetryOperation!, telemetryCollection, telemetryStopwatch.Elapsed.TotalMilliseconds * 1000, true, null, null);
                    }
                    catch (Exception)
                    {
                    }
#pragma warning restore CA1031
                }

                return response;
            }
            catch (RpcException ex)
            {
                // A caller-cancelled unary call surfaces as RpcException(Cancelled) via Grpc.Net.Client; surface it
                // as OperationCanceledException so standard `catch (OperationCanceledException)` callers are not
                // surprised, matching the streaming DumpMessages path. Only map when the caller's token was actually
                // cancelled: a server/transport-initiated CANCELLED must stay a MilvusException so `catch
                // (MilvusException)` callers do not miss it.
                if (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ex.Status.Detail, ex, cancellationToken);
                }

                // Transport-level failures surface as RpcException; wrap them into MilvusException for a
                // consistent public error surface (design doc §3.4), preserving the original gRPC status code.
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, $"RPC failed: {ex.StatusCode} {ex.Status.Detail}",
                    ex.StatusCode, ex);
            }
        }
        catch (Exception ex)
        {
            if (telemetryStopwatch is not null)
            {
#pragma warning disable CA1031 // Telemetry must never throw into the caller; the RPC result is authoritative.
                try
                {
                    _telemetry.RecordOperation(telemetryOperation!, telemetryCollection, telemetryStopwatch.Elapsed.TotalMilliseconds * 1000, false, ex.Message, Guid.NewGuid().ToString("N"));
                }
                catch (Exception)
                {
                }
#pragma warning restore CA1031
            }

            throw;
        }
    }

    private static string? ToTelemetryOperation(string callerName)
        => callerName switch
        {
            "InsertAsync" => "Insert",
            "DeleteAsync" => "Delete",
            "UpsertAsync" => "Upsert",
            "SearchAsync" => "Search",
            "HybridSearchAsync" => "HybridSearch",
            "QueryAsync" => "Query",
            "RunAnalyzerAsync" => "RunAnalyzer",
            _ => null
        };

    // Runs a logical DML/DQL operation (which may internally re-enter the facade, e.g. a schema-mismatch
    // retry) and records exactly one telemetry metric for its final outcome, mirroring the Java
    // recordLogicalOperation / C++ InvokeWithTelemetry wrappers. InvokeAsync suppresses per-attempt records
    // while LogicalOperationDepth > 0.
    private async Task<T> RecordLogicalOperationAsync<T>(
        string operation, string? collection, Func<Task<T>> supplier)
    {
        LogicalOperationDepth.Value++;
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            T result = await supplier().ConfigureAwait(false);
            RecordTelemetry(operation, collection, stopwatch, true, null);
            return result;
        }
        catch (Exception ex)
        {
            RecordTelemetry(operation, collection, stopwatch, false, ex.Message);
            throw;
        }
        finally
        {
            LogicalOperationDepth.Value--;
        }
    }

    private void RecordTelemetry(
        string operation, string? collection, System.Diagnostics.Stopwatch stopwatch, bool success, string? errorMessage)
    {
#pragma warning disable CA1031 // Telemetry must never throw into the caller; the RPC result is authoritative.
        try
        {
            _telemetry.RecordOperation(
                operation, collection, stopwatch.Elapsed.TotalMilliseconds * 1000, success, errorMessage,
                success ? null : Guid.NewGuid().ToString("N"));
        }
        catch (Exception)
        {
        }
#pragma warning restore CA1031
    }

    private static string? TryGetTelemetryCollection(object request)
        => request switch
        {
            Grpc.InsertRequest r => r.CollectionName,
            Grpc.DeleteRequest r => r.CollectionName,
            Grpc.UpsertRequest r => r.CollectionName,
            Grpc.SearchRequest r => r.CollectionName,
            Grpc.HybridSearchRequest r => r.CollectionName,
            Grpc.QueryRequest r => r.CollectionName,
            Grpc.RunAnalyzerRequest r => r.CollectionName,
            _ => null
        };
}
