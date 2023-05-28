using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
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
public partial class MilvusRestClient : IMilvusClient
{
    /// <summary>
    /// The constructor for the <see cref="MilvusRestClient"/>.
    /// </summary>
    /// <remarks>
    /// If you are using milvus managed by zilliz cloud, please use grpc client.
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
        Verify.ArgNotNullOrEmpty(endpoint, "Milvus client cannot be null or empty");

        this._log = log ?? NullLogger<MilvusRestClient>.Instance;
        this._httpClient = httpClient ?? new HttpClient();

        this._httpClient.BaseAddress = SanitizeEndpoint(endpoint, port);

        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{name}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue(
                "Basic", 
                authToken
            );
    }

    ///<inheritdoc/>
    public async Task<MilvusHealthState> HealthAsync(CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Ensure to connect to Milvus server {0}",Address);

        using HttpRequestMessage request = HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/health");

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Delete collection failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
            return new MilvusHealthState(true,"None",Grpc.ErrorCode.Success);

        var status = JsonSerializer.Deserialize<ResponseStatus>(responseContent);
        this._log.LogWarning(status.Reason);

        return new MilvusHealthState(status.ErrorCode == Grpc.ErrorCode.Success, status.Reason, status.ErrorCode);
    }

    #region Properties
    /// <summary>
    /// The endpoint for the Milvus service.
    /// </summary>
    public string Address => SanitizeEndpoint(this._httpClient.BaseAddress.ToString(),Port).ToString();

    /// <summary>
    /// The endpoint for the Milvus service.
    /// </summary>
    public string BaseAddress => this._httpClient.BaseAddress.ToString();

    /// <summary>
    /// The port for the Milvus service.
    /// </summary>
    public int Port => this._httpClient.BaseAddress.Port;
    #endregion

    /// <summary>
    /// Get the client msg;
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{{{nameof(MilvusRestClient)};{_httpClient.BaseAddress}}}";
    }

    #region private ================================================================================
    private ILogger _log;
    private HttpClient _httpClient;
    private bool _disposedValue;

    private async Task<(HttpResponseMessage response, string responseContent)> ExecuteHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
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
        Verify.IsValidUrl(nameof(endpoint), endpoint, false, true, false);

        UriBuilder builder = new(endpoint);
        if (port.HasValue) { builder.Port = port.Value; }

        return builder.Uri;
    }

    private void ValidateResponse(string responseContent)
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
    ///<inheritdoc/>
    public void Close()
    {
        Dispose();
    }

    ///<inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Close milvus connection.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
