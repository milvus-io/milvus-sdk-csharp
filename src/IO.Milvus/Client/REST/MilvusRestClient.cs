using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

/// <summary>
/// An implementation of a client for the Milvus VectorDB.
/// </summary>
public sealed partial class MilvusRestClient : IMilvusClient
{
    /// <summary>
    /// The constructor for the <see cref="MilvusRestClient"/>.
    /// </summary>
    /// <remarks>
    /// If you are using milvus managed by zilliz cloud, please use <see cref="gRPC.MilvusGrpcClient"/>
    /// </remarks>
    public MilvusRestClient(
        string endpoint,
        int port = 9091,
        string name = "root",
        string password = "milvus",
        HttpClient httpClient = null,
        ILogger log = null
        )
    {
        Verify.NotNullOrWhiteSpace(endpoint);

        _log = log ?? NullLogger<MilvusRestClient>.Instance;
        _httpClient = httpClient ?? s_defaultHttpClient;

        // Store the base address and auth header for all requests. These are added to each
        // HttpRequestMessage rather than to _httpClient to avoid mutating a shared HttpClient instance,
        // especially one that's provided by a consumer.
        _baseAddress = SanitizeEndpoint(endpoint, port);
        _authHeader = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{name}:{password}"))
            );
    }

    #region Properties
    /// <summary>
    /// The endpoint for the Milvus service.
    /// </summary>
    public string Address => _baseAddress.ToString();

    /// <summary>
    /// The port for the Milvus service.
    /// </summary>
    public int Port => _baseAddress.Port;
    #endregion

    ///<inheritdoc/>
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Ensure to connect to Milvus server {0}", Address);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/health");

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.AsSpan().Trim().SequenceEqual("{}".AsSpan()))
            return new MilvusHealthState(true, "None", Grpc.ErrorCode.Success);

        var status = JsonSerializer.Deserialize<ResponseStatus>(responseContent);
        _log.LogWarning("Reason: {0}", status.Reason);

        return new MilvusHealthState(status.ErrorCode == Grpc.ErrorCode.Success, status.Reason, status.ErrorCode);
    }


    ///<inheritdoc/>
    public Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    /// <summary>
    /// Get the client msg;
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{{{nameof(MilvusRestClient)};{_baseAddress}}}";
    }

    #region private ================================================================================

    /// <summary>Default HttpClient instance used if none is provided to a <see cref="MilvusRestClient"/> instance.</summary>
    private static readonly HttpClient s_defaultHttpClient = new HttpClient();

    private readonly Uri _baseAddress;
    private readonly AuthenticationHeaderValue _authHeader;
    private readonly ILogger _log;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private async Task<string> ExecuteHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerName = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        request.RequestUri = new Uri(_baseAddress, request.RequestUri);
        request.Headers.Authorization = _authHeader;

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Milvus {0} request: {1}", callerName, request);
        }

        string responseContent = null;
        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (_log.IsEnabled(LogLevel.Trace))
            {
                _log.LogTrace("Milvus response: {statusCode}", response.StatusCode);
            }

            responseContent = await response.Content.ReadAsStringAsync(
#if NET6_0_OR_GREATER
                    cancellationToken
#endif
                    ).ConfigureAwait(false);


            response.EnsureSuccessStatusCode();

            return responseContent;
        }
        catch (Exception e)
        {
            if (responseContent is not null)
            {
                e.Data[nameof(responseContent)] = responseContent;
                _log.LogError(e, "{0} failed: {1}, {2}", callerName, e.Message, responseContent);
            }
            else
            {
                _log.LogError(e, "{0} failed: {1}", callerName, e.Message);
            }
            throw;
        }
    }

    private static Uri SanitizeEndpoint(string endpoint, int? port)
    {
        Verify.ValidUrl(nameof(endpoint), endpoint, false, true, false);

        UriBuilder builder = new(endpoint);
        if (port.HasValue) { builder.Port = port.Value; }

        return builder.Uri;
    }

    private void ValidateResponse(string responseContent, [CallerMemberName] string callerName = null)
    {
        if (!string.IsNullOrWhiteSpace(responseContent) &&
            !responseContent.AsSpan().Trim().SequenceEqual("{}".AsSpan()))
        {
            ValidateStatus(JsonSerializer.Deserialize<ResponseStatus>(responseContent), callerName);
        }
    }

    private void ValidateStatus(ResponseStatus status, [CallerMemberName] string callerName = null)
    {
        if (status is not null && status.ErrorCode is not Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed {0}: {1}, {2}", callerName, status.ErrorCode, status.Reason);
            throw new MilvusException(status);
        }
    }
    #endregion

    #region IDisposable Support

    /// <summary>
    /// Close milvus connection.
    /// </summary>
    public void Dispose()
    {
        // The HttpClient provided to this MilvusRestClient is either externally owned, in which case
        // we don't want to dispose of it here, or it's the static HttpClient instance shared by any
        // number of MilvusRestClient instances, in which case we also don't want to dispose of it here.
        // Simply mark this instance as no longer being usable.
        _disposed = true;
    }

    ///<inheritdoc/>
    public void Close()
    {
        Dispose();
    }
    #endregion
}
