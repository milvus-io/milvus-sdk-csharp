using System.Diagnostics;
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

        _grpcChannel = CreateDefaultChannel(config);
        GrpcClient = new Grpc.MilvusService.MilvusServiceClient(_grpcChannel);

        Debug.Assert(config.ApiKey is null || config.Username is null);

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
        _retryConfig = config.Retry ?? new RetryConfig();
        _endpoint = TryGetEndpoint(config.Uri);
        _database = config.Database ?? "default";
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
                await _connectTask.ConfigureAwait(false);
                return;
            }

            _connectTask = ConnectRpcAsync(cancellationToken);
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
            await _connectTask.ConfigureAwait(false);
            return;
        }

        await ConnectAsync(cancellationToken).ConfigureAwait(false);
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
                MilvusErrorCode.UnexpectedError, $"Failed to connect to Milvus: {ex.StatusCode} {ex.Status.Detail}");
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

        Grpc.CheckHealthResponse response =
            await GrpcClient.CheckHealthAsync(new Grpc.CheckHealthRequest(),
                    CreateCallOptions(cancellationToken))
                .ConfigureAwait(false);

        if (!response.IsHealthy)
        {
            _log.HealthCheckFailed(response.Reasons);
        }

        return new MilvusHealthState(response.IsHealthy, response.Status.Reason,
            (MilvusErrorCode)response.Status.Code);
    }

    /// <summary>
    /// Checks the health of the Milvus server (alias of <see cref="HealthAsync" />, matching the design doc §4.12
    /// API name).
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public Task<MilvusHealthState> CheckHealthAsync(CancellationToken cancellationToken = default)
        => HealthAsync(cancellationToken);

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

    private static GrpcChannelOptions CreateDefaultChannelOptions(ILoggerFactory? loggerFactory)
    {
        var options = new GrpcChannelOptions { LoggerFactory = loggerFactory };

#if NET5_0_OR_GREATER
        options.HttpHandler = new SocketsHttpHandler
        {
            KeepAlivePingDelay = TimeSpan.FromSeconds(10),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
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

        return GrpcChannel.ForAddress(uri, config.ChannelOptions ?? CreateDefaultChannelOptions(config.LoggerFactory));
    }

    private readonly ILogger _log;
    private readonly GrpcChannel _grpcChannel;
    private readonly string _username;
    private readonly RetryConfig _retryConfig;
    private readonly string _endpoint;
    private readonly TimeSpan? _connectTimeout;
    private string _database;
    private string? _authorizationHeader;

    private CallOptions _callOptions;

    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private Task? _connectTask;

    internal Grpc.MilvusService.MilvusServiceClient GrpcClient { get; }

    internal CallOptions CallOptionsForStreaming(CancellationToken cancellationToken)
        => CreateCallOptions(cancellationToken);

    // Applies a fresh ConnectTimeout deadline on every call, rather than a fixed absolute deadline captured once
    // at construction time (which would expire and break every later call on a long-lived client).
    private CallOptions CreateCallOptions(CancellationToken cancellationToken)
    {
        CallOptions options = _callOptions.WithCancellationToken(cancellationToken);
        if (_connectTimeout is { } timeout)
        {
            options = options.WithDeadline(DateTime.UtcNow.Add(timeout));
        }

        return options;
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
        _log.OperationInvoked(callerName, request);

        try
        {
            return await RetryPolicy.ExecuteAsync(
                async innerCt =>
                {
                    TResponse response = await func(request, CreateCallOptions(innerCt)).ConfigureAwait(false);
                    Grpc.Status status = getStatus(response);
                    var code = (MilvusErrorCode)status.Code;

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
            // Transport-level failures surface as RpcException; wrap them into MilvusException for a
            // consistent public error surface (design doc §3.4). Server errors remain MilvusException.
            throw new MilvusException(
                MilvusErrorCode.UnexpectedError, $"RPC failed: {ex.StatusCode} {ex.Status.Detail}");
        }
    }
}
