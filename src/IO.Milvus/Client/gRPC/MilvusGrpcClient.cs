using Grpc.Core;
using Grpc.Net.Client;
using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

/// <summary>
/// Milvus gRPC client
/// </summary>
public sealed partial class MilvusGrpcClient : IMilvusClient
{
    /// <summary>
    /// The constructor for the <see cref="MilvusGrpcClient"/>
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="port"></param>
    /// <param name="name"></param>
    /// <param name="password"></param>
    /// <param name="grpcChannel"></param>
    /// <param name="callOptions"></param>
    /// <param name="log"></param>
    public MilvusGrpcClient(
        string endpoint,
        int port = 19530,
        string name = "root",
        string password = "milvus",
        GrpcChannel grpcChannel = null,
        CallOptions? callOptions = default,
        ILogger log = null):
        this(endpoint, port, $"{name}:{password}", grpcChannel, callOptions, log)
    { }

    /// <summary>
    /// The constructor for the <see cref="MilvusGrpcClient"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see href="https://docs.zilliz.com/docs/connect-to-cluster"/> 
    /// </para>
    /// </remarks>
    /// <param name="endpoint">Endpoint.</param>
    /// <param name="port"></param>
    /// <param name="authorization">Authorization.</param>
    /// <param name="grpcChannel">Optional.</param>
    /// <param name="callOptions">Optional.</param>
    /// <param name="log">Optional.</param>
    public MilvusGrpcClient(
        string endpoint,
        int? port,
        string authorization,
        GrpcChannel grpcChannel = null,
        CallOptions? callOptions = default,
        ILogger log = null)
    {
        Verify.NotNull(endpoint);

        Uri address = SanitizeEndpoint(endpoint, port);

        _log = log ?? NullLogger<MilvusGrpcClient>.Instance;
        if (grpcChannel is not null)
        {
            _grpcChannel = grpcChannel;
            _ownsGrpcChannel = false;
        }
        else
        {
            _grpcChannel = GrpcChannel.ForAddress(address);
            _ownsGrpcChannel = true;
        }

        _callOptions = callOptions ?? new CallOptions(
            new Metadata()
            {
                { "authorization", Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization)) }
            });

        _grpcClient = new MilvusService.MilvusServiceClient(_grpcChannel);
    }

    /// <inheritdoc />
    public string Address => _grpcChannel.Target;

    /// <inheritdoc />
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        CheckHealthResponse response = await InvokeAsync(_grpcClient.CheckHealthAsync, new CheckHealthRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        if (_log.IsEnabled(LogLevel.Warning))
        {
            if (!response.IsHealthy)
            {
                _log.LogWarning("Unhealthy: {0}", string.Join(", ", response.Reasons));
            }
        }

        return new MilvusHealthState(response.IsHealthy, response.Status.Reason, response.Status.ErrorCode);
    }

    /// <inheritdoc />
    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        GetVersionResponse response = await InvokeAsync(_grpcClient.GetVersionAsync, new GetVersionRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Version;
    }

    /// <inheritdoc />
    public override string ToString() => $"{{{nameof(MilvusGrpcClient)}:{Address}}}";

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

    #region Private ===============================================================================
    private readonly ILogger _log;
    private readonly GrpcChannel _grpcChannel;
    private readonly CallOptions _callOptions;
    private readonly MilvusService.MilvusServiceClient _grpcClient;
    private readonly bool _ownsGrpcChannel;

    private static Uri SanitizeEndpoint(string endpoint, int? port)
    {
        Verify.ValidUrl(nameof(endpoint), endpoint, false, true, false);

        UriBuilder builder = new(endpoint);
        if (port.HasValue) { builder.Port = port.Value; }

        return builder.Uri;
    }

    private Task<Grpc.Status> InvokeAsync<TRequest>(
        Func<TRequest, CallOptions, AsyncUnaryCall<Grpc.Status>> func,
        TRequest request,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = null) =>
        InvokeAsync(func, request, r => r, cancellationToken, callerName);

    private async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        Func<TRequest, CallOptions, AsyncUnaryCall<TResponse>> func,
        TRequest request,
        Func<TResponse, Grpc.Status> getStatus,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerName = null)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("{0} invoked: {1}", callerName, request);
        }

        TResponse response = await func(request, _callOptions.WithCancellationToken(cancellationToken)).ConfigureAwait(false);
        Grpc.Status status = getStatus(response);

        if (status.ErrorCode != ErrorCode.Success)
        {
            if (_log.IsEnabled(LogLevel.Error))
            {
                _log.LogError("{0} failed: {1}, {2}", callerName, status.ErrorCode, status.Reason);
            }

            throw new MilvusException(MilvusException.GetErrorMessage(status.ErrorCode, status.Reason));
        }

        return response;
    }

    private static string Base64Encode(string input) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    #endregion
}