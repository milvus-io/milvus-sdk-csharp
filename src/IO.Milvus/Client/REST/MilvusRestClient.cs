using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
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

        this._log = log ?? NullLogger<MilvusRestClient>.Instance;
        this._httpClient = httpClient ?? s_defaultHttpClient;

        // Store the base address and auth header for all requests. These are added to each
        // HttpRequestMessage rather than to _httpClient to avoid mutating a shared HttpClient instance,
        // especially one that's provided by a consumer.
        this._baseAddress = SanitizeEndpoint(endpoint, port);
        this._authHeader = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{name}:{password}"))
            );
    }

    #region Properties
    /// <summary>
    /// The endpoint for the Milvus service.
    /// </summary>
    public string Address => this._baseAddress.ToString();

    /// <summary>
    /// The port for the Milvus service.
    /// </summary>
    public int Port => this._baseAddress.Port;
    #endregion

    ///<inheritdoc/>
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Ensure to connect to Milvus server {0}", Address);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/health");

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Ensure to connect to Milvus server failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
            return new MilvusHealthState(true, "None", Grpc.ErrorCode.Success);

        var status = JsonSerializer.Deserialize<ResponseStatus>(responseContent);
        this._log.LogWarning("Reason: {0}", status.Reason);

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
        return $"{{{nameof(MilvusRestClient)};{this._baseAddress}}}";
    }

    #region private ================================================================================

    /// <summary>Default HttpClient instance used if none is provided to a <see cref="MilvusRestClient"/> instance.</summary>
    private static readonly HttpClient s_defaultHttpClient = new HttpClient();

    private readonly Uri _baseAddress;
    private readonly AuthenticationHeaderValue _authHeader;
    private readonly ILogger _log;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private async Task<(HttpResponseMessage response, string responseContent)> ExecuteHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        request.RequestUri = new Uri(this._baseAddress, request.RequestUri);
        request.Headers.Authorization = this._authHeader;

        HttpResponseMessage response = await this._httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        string responseContent = await response
            .Content
            .ReadAsStringAsync()
            .ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            this._log.LogTrace("Milvus responded successfully");
        }
        else
        {
            this._log.LogTrace("Milvus responded with error");
        }

        return (response, responseContent);
    }

    private static Uri SanitizeEndpoint(string endpoint, int? port)
    {
        Verify.ValidUrl(nameof(endpoint), endpoint, false, true, false);

        UriBuilder builder = new(endpoint);
        if (port.HasValue) { builder.Port = port.Value; }

        return builder.Uri;
    }

    private static void ValidateResponse(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
            return;

        var status = JsonSerializer.Deserialize<ResponseStatus>(responseContent);
        if (status.ErrorCode != Grpc.ErrorCode.Success)
        {
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
