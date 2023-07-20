using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;

namespace IO.Milvus.Client;

/// <summary>
/// Milvus gRPC client
/// </summary>
public sealed partial class MilvusClient : IDisposable
{
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
    /// <param name="log">An optional logger through which the Milvus client will log.</param>
    public MilvusClient(
        string address,
        string username,
        string password,
        CallOptions callOptions = default,
        ILogger? log = null)
        : this(new Uri(address), username, password, callOptions, log)
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
    /// <param name="log">An optional logger through which the Milvus client will log.</param>
    public MilvusClient(
        Uri address,
        string username,
        string password,
        CallOptions callOptions = default,
        ILogger? log = null)
        : this(GrpcChannel.ForAddress(address), ownsGrpcChannel: true, username, password, callOptions, log)
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
    /// <param name="log">An optional logger through which the Milvus client will log.</param>
    public MilvusClient(
        GrpcChannel grpcChannel,
        string username,
        string password,
        CallOptions callOptions = default,
        ILogger? log = null)
        : this(grpcChannel, ownsGrpcChannel: false, username, password, callOptions, log)
    {
    }

    private MilvusClient(
        GrpcChannel grpcChannel,
        bool ownsGrpcChannel,
        string username,
        string password,
        CallOptions callOptions = default,
        ILogger? log = null)
    {
        Verify.NotNull(grpcChannel);

        _grpcChannel = grpcChannel;
        _grpcClient = new MilvusService.MilvusServiceClient(_grpcChannel);
        _ownsGrpcChannel = ownsGrpcChannel;

        _callOptions = callOptions.WithHeaders(new Metadata
        {
            { "authorization", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")) }
        });

        _log = log ?? NullLogger<MilvusClient>.Instance;
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
        CheckHealthResponse response = await InvokeAsync(_grpcClient.CheckHealthAsync, new CheckHealthRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        if (!response.IsHealthy)
        {
            _log.HealthCheckFailed(response.Reasons);
        }

        return new MilvusHealthState(response.IsHealthy, response.Status.Reason, response.Status.ErrorCode);
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
        GetVersionResponse response = await InvokeAsync(_grpcClient.GetVersionAsync, new GetVersionRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

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
    private readonly MilvusService.MilvusServiceClient _grpcClient;
    private readonly bool _ownsGrpcChannel;

    private Task<Grpc.Status> InvokeAsync<TRequest>(
        Func<TRequest, CallOptions, AsyncUnaryCall<Grpc.Status>> func,
        TRequest request,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = "")
        where TRequest : class
        => InvokeAsync(func, request, r => r, cancellationToken, callerName);

    private async Task<TResponse> InvokeAsync<TRequest, TResponse>(
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

        if (status.ErrorCode != ErrorCode.Success)
        {
            _log.OperationFailed(callerName, status.ErrorCode, status.Reason);

            throw new MilvusException(status.ErrorCode, status.Reason);
        }

        return response;
    }

    private static string Base64Encode(string input) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
}
