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
    /// <summary>
    /// Creates a new <see cref="MilvusClientV2" />, connecting to the given Milvus instance.
    /// </summary>
    /// <param name="config">The connection parameters, mirroring the <c>ConnectParam</c> of the C++ SDK and the
    /// <c>ConnectConfig</c> of the Java SDK.</param>
    public MilvusClientV2(ConnectConfig config)
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

        _grpcChannel = CreateDefaultChannel(config);
        GrpcClient = new Grpc.MilvusService.MilvusServiceClient(_grpcChannel);

        string? authorization = config.Username is null
            ? config.ApiKey
            : $"{config.Username}:{config.Password}";

        var metadata = new Metadata();

        if (authorization is not null)
        {
            _authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization));
            metadata.Add("authorization", _authorizationHeader);
        }

        if (config.Database is not null)
        {
            metadata.Add("dbname", config.Database);
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
        _endpoint = TryGetEndpoint(config.Uri);
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
        var request = new Grpc.ConnectRequest
        {
            ClientInfo = new Grpc.ClientInfo
            {
                SdkType = "CSharp",
                SdkVersion = typeof(MilvusClientV2).Assembly.GetName().Version?.ToString() ?? "unknown",
                User = _username,
                Host = TryGetUriHost(_grpcChannel.Target),
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
        }
        catch (RpcException ex)
        {
            // Transport/auth failures surface here (explicit ConnectAsync or lazy first call).
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, $"Failed to connect to Milvus: {ex.StatusCode} {ex.Status.Detail}",
                ex.StatusCode, ex);
        }
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
    public string Address => _grpcChannel.Target;

    /// <summary>
    /// Checks the health of the Milvus server.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.CheckHealthResponse response = await InvokeAsync(
                GrpcClient.CheckHealthAsync, new Grpc.CheckHealthRequest(), static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsHealthy)
        {
            _log.HealthCheckFailed(response.Reasons);
        }

        return new MilvusHealthState(response.IsHealthy, response.Status.Reason,
            (MilvusErrorCode)response.Status.Code,
            response.Reasons.ToList(),
            response.QuotaStates.Select(q => (QuotaState)(int)q).ToList());
    }

    /// <summary>
    /// Checks the health of the Milvus server (alias of <see cref="HealthAsync" />, matching the design doc §4.12
    /// API name).
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task<MilvusHealthState> CheckHealthAsync(CancellationToken cancellationToken = default)
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
        _grpcChannel.Dispose();
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

    private readonly ILogger _log;
    private readonly GrpcChannel _grpcChannel;
    private readonly string _username;
    private string _password;
    private readonly RetryConfig _retryConfig;
    private readonly string _endpoint;
    private readonly TimeSpan? _connectTimeout;
    private string _database;
    private string? _authorizationHeader;

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

    internal Grpc.MilvusService.MilvusServiceClient GrpcClient { get; }

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
        if (_username.Length == 0 || !string.Equals(_username, changedUserName, StringComparison.Ordinal)
            || newPassword is null)
        {
            return;
        }

        lock (_connectLock)
        {
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
        // RBAC credential requests carry password material that must not be written to the debug log.
        _log.OperationInvoked(
            callerName,
            request is Requests.Rbac.CreateUserReq or Requests.Rbac.UpdatePasswordReq
                ? new { credential = "redacted" }
                : request);

        try
        {
            return await RetryPolicy.ExecuteAsync(
                async innerCt =>
                {
                    TResponse response = await func(request, CreateCallOptions(innerCt)).ConfigureAwait(false);
                    Grpc.Status status = getStatus(response);
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

                    return response;
                },
                _retryConfig,
                cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            // A caller-cancelled unary call surfaces as RpcException(Cancelled) via Grpc.Net.Client; surface it
            // as OperationCanceledException so standard `catch (OperationCanceledException)` callers are not
            // surprised, matching the streaming DumpMessages path.
            if (ex.StatusCode == StatusCode.Cancelled)
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
}
