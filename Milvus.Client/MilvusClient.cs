﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Milvus.Client.Diagnostics;

namespace Milvus.Client;

/// <summary>
/// Milvus gRPC client
/// </summary>
public sealed partial class MilvusClient : IDisposable
{
    private const int DefaultMilvusPort = 19530;

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />, connecting to the given hostname on the default Milvus port of 19530.
    /// </summary>
    /// <param name="host">The hostname or IP address to connect to.</param>
    /// <param name="port">The port to connect to. Defaults to 19530.</param>
    /// <param name="ssl">Whether to use TLS/SSL. Defaults to <c>false</c>.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        string host,
        int port = DefaultMilvusPort,
        bool ssl = false,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(new UriBuilder(ssl ? "https" : "http", host, port).Uri, database, callOptions, loggerFactory)
    {
        Verify.NotNull(host);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />, connecting to the given hostname on the default Milvus port of 19530.
    /// </summary>
    /// <param name="host">The hostname or IP address to connect to.</param>
    /// <param name="username">The username to use for authentication.</param>
    /// <param name="password">The password to use for authentication.</param>
    /// <param name="port">The port to connect to. Defaults to 19530.</param>
    /// <param name="ssl">Whether to use TLS/SSL. Defaults to <c>false</c>.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        string host,
        string username,
        string password,
        int port = DefaultMilvusPort,
        bool ssl = false,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(
            new UriBuilder(ssl ? "https" : "http", host, port).Uri,
            username, password, database, callOptions, loggerFactory)
    {
        Verify.NotNull(host);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />, connecting to the given hostname on the default Milvus port of 19530.
    /// </summary>
    /// <param name="host">The hostname or IP address to connect to.</param>
    /// <param name="apiKey">An API key to be used for authentication, instead of a username and password.</param>
    /// <param name="port">The port to connect to. Defaults to 19530.</param>
    /// <param name="ssl">Whether to use TLS/SSL. Defaults to <c>false</c>.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        string host,
        string apiKey,
        int port = DefaultMilvusPort,
        bool ssl = false,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(new UriBuilder(ssl ? "https" : "http", host, port).Uri, apiKey, database, callOptions, loggerFactory)
    {
        Verify.NotNull(host);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="endpoint">
    /// A URI to use for connecting to Milvus via gRPC. Must be a valid URI containing the port (the default Milvus gRPC
    /// port is 19530).
    /// </param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        Uri endpoint,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(
            GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions { LoggerFactory = loggerFactory }),
            ownsGrpcChannel: true,
            username: null,
            password: null,
            apiKey: null,
            database,
            callOptions,
            loggerFactory)
    {
        Verify.NotNull(endpoint);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="endpoint">
    /// A URI to use for connecting to Milvus via gRPC. Must be a valid URI containing the port (the default Milvus gRPC
    /// port is 19530).
    /// </param>
    /// <param name="username">The username to use for authentication.</param>
    /// <param name="password">The password to use for authentication.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        Uri endpoint,
        string username,
        string password,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(
            GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions { LoggerFactory = loggerFactory }),
            ownsGrpcChannel: true,
            username,
            password,
            apiKey: null,
            database,
            callOptions,
            loggerFactory)
    {
        Verify.NotNull(endpoint);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="endpoint">
    /// A URI to use for connecting to Milvus via gRPC. Must be a valid URI containing the port (the default Milvus gRPC
    /// port is 19530).
    /// </param>
    /// <param name="apiKey">An API key to be used for authentication, instead of a username and password.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        Uri endpoint,
        string apiKey,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(
            GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions { LoggerFactory = loggerFactory }),
            ownsGrpcChannel: true,
            username: null,
            password: null,
            apiKey,
            database,
            callOptions,
            loggerFactory)
    {
        Verify.NotNull(endpoint);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="grpcChannel">The gRPC channel to use for connecting to Milvus.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        GrpcChannel grpcChannel,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(grpcChannel, ownsGrpcChannel: false, username: null, password: null, apiKey: null, database, callOptions,
            loggerFactory)
    {
        Verify.NotNull(grpcChannel);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="grpcChannel">The gRPC channel to use for connecting to Milvus.</param>
    /// <param name="username">The username to use for authentication.</param>
    /// <param name="password">The password to use for authentication.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        GrpcChannel grpcChannel,
        string username,
        string password,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(grpcChannel, ownsGrpcChannel: false, username, password, apiKey: null, database, callOptions,
            loggerFactory)
    {
        Verify.NotNull(grpcChannel);
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="grpcChannel">The gRPC channel to use for connecting to Milvus.</param>
    /// <param name="apiKey">An API key to be used for authentication, instead of a username and password.</param>
    /// <param name="database">The database to connect to. Defaults to the default Milvus database.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        GrpcChannel grpcChannel,
        string apiKey,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(grpcChannel, ownsGrpcChannel: false, username: null, password: null, apiKey, database, callOptions,
            loggerFactory)
    {
        Verify.NotNull(grpcChannel);
    }

    private MilvusClient(
        GrpcChannel grpcChannel,
        bool ownsGrpcChannel,
        string? username = null,
        string? password = null,
        string? apiKey = null,
        string? database = null,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(grpcChannel);

        _grpcChannel = grpcChannel;
        GrpcClient = new MilvusService.MilvusServiceClient(_grpcChannel);
        _ownsGrpcChannel = ownsGrpcChannel;

        Debug.Assert(apiKey is null || username is null);

        string? authorization = username is null
            ? apiKey
            : $"{username}:{password}";

        var metadata = new Metadata();

        if (authorization is not null)
        {
            metadata.Add("authorization", Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization)));
        }

        if (database is not null)
        {
            metadata.Add("dbname", database);
        }

        if (metadata.Count > 0)
        {
            _callOptions = callOptions.WithHeaders(metadata);
        }

        _log = loggerFactory?.CreateLogger("Milvus.Client") ?? NullLogger.Instance;
    }

    /// <summary>
    /// Base address of Milvus server.
    /// </summary>
    public string Address => _grpcChannel.Target;

    /// <summary>
    /// Ensure to connect to Milvus server before any operations.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        CheckHealthResponse response =
            await GrpcClient.CheckHealthAsync(new CheckHealthRequest(),
                    _callOptions.WithCancellationToken(cancellationToken))
                .ConfigureAwait(false);

        if (!response.IsHealthy)
        {
            _log.HealthCheckFailed(response.Reasons);
        }

        return new MilvusHealthState(response.IsHealthy, response.Status.Reason, (MilvusErrorCode)response.Status.Code);
    }

    /// <summary>
    /// Get Milvus version.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        GetVersionResponse response = await InvokeAsync(GrpcClient.GetVersionAsync, new GetVersionRequest(),
            static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Version;
    }

    /// <inheritdoc />
    public override string ToString() => $"{{{nameof(MilvusClient)}:{Address}}}";

    /// <summary>
    /// Close milvus connection.
    /// </summary>
    public void Dispose()
    {
        if (_ownsGrpcChannel)
        {
            _grpcChannel.Dispose();
        }
    }

    private readonly ILogger _log;
    private readonly GrpcChannel _grpcChannel;
    private readonly bool _ownsGrpcChannel;

    private CallOptions _callOptions;

    internal MilvusService.MilvusServiceClient GrpcClient { get; }

    internal Task<Grpc.Status> InvokeAsync<TRequest>(
        Func<TRequest, CallOptions, AsyncUnaryCall<Grpc.Status>> func,
        TRequest request,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = "")
        where TRequest : class
        => InvokeAsync(func, request, r => r, cancellationToken, callerName);

    internal async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        Func<TRequest, CallOptions, AsyncUnaryCall<TResponse>> func,
        TRequest request,
        Func<TResponse, Grpc.Status> getStatus,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = "")
        where TRequest : class
    {
        _log.OperationInvoked(callerName, request);

        TResponse response = await func(request, _callOptions.WithCancellationToken(cancellationToken)).ConfigureAwait(false);
        Grpc.Status status = getStatus(response);
        var code = (MilvusErrorCode)status.Code;

        if (code != MilvusErrorCode.Success)
        {
            _log.OperationFailed(callerName, code, status.Reason);

            throw new MilvusException(code, status.Reason);
        }

        return response;
    }
}
