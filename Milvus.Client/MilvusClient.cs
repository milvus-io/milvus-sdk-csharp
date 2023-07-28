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
    private readonly MilvusDatabase _defaultDatabase;

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="address">
    /// A URI to use for connecting to Milvus via gRPC. Must be a valid URI containing the port (the default Milvus gRPC
    /// port is 19530).
    /// </param>
    /// <param name="username">The username to use for authentication.</param>
    /// <param name="password">The password to use for authentication.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        string address,
        string username,
        string password,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(new Uri(address), username, password, callOptions, loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="address">
    /// A URI to use for connecting to Milvus via gRPC. Must be a valid URI containing the port (the default Milvus gRPC
    /// port is 19530).
    /// </param>
    /// <param name="username">The username to use for authentication.</param>
    /// <param name="password">The password to use for authentication.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        Uri address,
        string username,
        string password,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(GrpcChannel.ForAddress(address), ownsGrpcChannel: true, username, password, callOptions, loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="MilvusClient" />.
    /// </summary>
    /// <param name="grpcChannel">The gRPC channel to use for connecting to Milvus.</param>
    /// <param name="username">The username to use for authentication.</param>
    /// <param name="password">The password to use for authentication.</param>
    /// <param name="callOptions">
    /// Optional gRPC call options to pass by default when sending requests, e.g. the default deadline.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory through which the Milvus client will log.</param>
    public MilvusClient(
        GrpcChannel grpcChannel,
        string username,
        string password,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
        : this(grpcChannel, ownsGrpcChannel: false, username, password, callOptions, loggerFactory)
    {
    }

    private MilvusClient(
        GrpcChannel grpcChannel,
        bool ownsGrpcChannel,
        string username,
        string password,
        CallOptions callOptions = default,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(grpcChannel);

        _grpcChannel = grpcChannel;
        GrpcClient = new MilvusService.MilvusServiceClient(_grpcChannel);
        _ownsGrpcChannel = ownsGrpcChannel;

        _callOptions = callOptions.WithHeaders(new Metadata
        {
            { "authorization", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")) }
        });

        _log = loggerFactory?.CreateLogger("Milvus.Client") ?? NullLogger.Instance;

        _defaultDatabase = new MilvusDatabase(this, databaseName: null);
    }

    /// <summary>
    /// Base address of Milvus server.
    /// </summary>
    public string Address => _grpcChannel.Target;

    /// <summary>
    /// Ensure to connect to Milvus server before any operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        CheckHealthResponse response = await InvokeAsync(GrpcClient.CheckHealthAsync, new CheckHealthRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        if (!response.IsHealthy)
        {
            _log.HealthCheckFailed(response.Reasons);
        }

        return new MilvusHealthState(response.IsHealthy, response.Status.Reason, (MilvusErrorCode)response.Status.ErrorCode);
    }

    /// <summary>
    /// Get Milvus version.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>Milvus version</returns>
    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        GetVersionResponse response = await InvokeAsync(GrpcClient.GetVersionAsync, new GetVersionRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

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
    private readonly CallOptions _callOptions;
    private readonly bool _ownsGrpcChannel;

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

        if (status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.OperationFailed(callerName, (MilvusErrorCode)status.ErrorCode, status.Reason);

            throw new MilvusException((MilvusErrorCode)status.ErrorCode, status.Reason);
        }

        return response;
    }
}
