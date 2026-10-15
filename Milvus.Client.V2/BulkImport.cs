using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

#pragma warning disable CA1056 // Url is a plain string to mirror the C++/Java/Rust BulkImport APIs.

/// <summary>
/// Connection settings for the Milvus bulk-import REST client. Mirrors the Rust SDK's <c>BulkImportConfig</c>.
/// </summary>
public sealed class BulkImportConfig
{
    /// <summary>
    /// The Milvus HTTP (REST) endpoint, e.g. <c>http://localhost:19530</c>.
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// The API key/token, sent verbatim as an <c>Authorization: Bearer &lt;apiKey&gt;</c> header.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The per-request HTTP timeout. Defaults to 20 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
}

/// <summary>
/// A reusable Milvus bulk-import REST client. Bulk import is served by Milvus's HTTP API rather than its gRPC
/// service; construct from a <see cref="BulkImportConfig" />, then create, list or describe import jobs with
/// request objects (never piecemeal parameters). Mirrors the Rust SDK's <c>BulkImport</c> for the subset of
/// import endpoints that exist on the 2.6 server (commit/abort import are not part of the 2.6 API surface).
/// </summary>
public sealed class BulkImport : IDisposable
{
    /// <summary>
    /// The <see cref="HttpMessageHandler" /> used for REST calls. Tests inject a handler that captures the
    /// request; the default lazily creates an <see cref="HttpClientHandler" />.
    /// </summary>
    internal static HttpMessageHandler? HttpMessageHandler { get; set; }

    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly string? _apiKey;

    /// <summary>
    /// Creates a new bulk-import REST client.
    /// </summary>
    /// <param name="config">The connection settings.</param>
    public BulkImport(BulkImportConfig config)
    {
        Verify.NotNull(config);
        Verify.NotNullOrWhiteSpace(config.Url);

        _baseUrl = config.Url.TrimEnd('/');
        _apiKey = config.ApiKey;

        // Apply config.Timeout on both paths: an injected handler keeps the HttpClient's 100-second default
        // otherwise, contradicting the documented per-request timeout.
        _client = HttpMessageHandler is null
            ? new HttpClient { Timeout = config.Timeout }
            : new HttpClient(HttpMessageHandler, disposeHandler: false) { Timeout = config.Timeout };
    }

    /// <summary>
    /// Creates an import job from the given data files (or cloud object/volume sources).
    /// </summary>
    public async Task<BulkImportResp> BulkImportAsync(
        BulkImportRequest request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        return await PostAsync("/v2/vectordb/jobs/import/create", request.DatabaseName, request.ToBody(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the import jobs of a collection (or all collections when
    /// <see cref="ListImportJobsRequest.CollectionName" /> is empty).
    /// </summary>
    public async Task<BulkImportResp> ListImportJobsAsync(
        ListImportJobsRequest request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        return await PostAsync("/v2/vectordb/jobs/import/list", request.DatabaseName, request.ToBody(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the progress of an import job.
    /// </summary>
    public async Task<BulkImportResp> GetImportProgressAsync(
        GetImportProgressRequest request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        return await PostAsync("/v2/vectordb/jobs/import/get_progress", request.DatabaseName, request.ToBody(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    private async Task<BulkImportResp> PostAsync(
        string path, string databaseName, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + path);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        // The import progress/commit/abort endpoints bind only the job id from the body and derive the database
        // from the "DB-Name" header (the JobIDReq on the server does not implement DBNameGetter), so always
        // carry the database there too.
        request.Headers.TryAddWithoutValidation("DB-Name", databaseName);

        string body;
        try
        {
            using HttpResponseMessage response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
#if NET5_0_OR_GREATER
            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode)
            {
                throw new MilvusException(MilvusErrorCode.UnexpectedError,
                    $"Bulk import REST call to '{path}' failed with HTTP {(int)response.StatusCode}: {body}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Bulk import REST call to '{path}' failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient's default 100s timeout surfaces as TaskCanceledException; surface it as a
            // MilvusException so it is not mistaken for the caller's own cancellation.
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Bulk import REST call to '{path}' timed out.", ex);
        }

        // The server renders business errors (missing parameters, invalid options, collection not found, ...)
        // as HTTP 200 with a non-zero "code" in the body; mirror the Java SDK's handleResponse and surface them.
        // A non-JSON 200 body (e.g. an HTML gateway/proxy error page) is a server/REST failure and must surface
        // as MilvusException too, consistent with every other failure path.
        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Bulk import REST call to '{path}' returned a non-JSON response: {body}", ex);
        }

        // Guard against a valid-JSON-but-not-an-object body (e.g. [], null, a bare string): RootElement
        // property access below would throw InvalidOperationException and leak the document.
        if (json.RootElement.ValueKind != JsonValueKind.Object)
        {
            json.Dispose();
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Bulk import REST call to '{path}' returned a non-object JSON response: {body}");
        }

        if (json.RootElement.TryGetProperty("code", out JsonElement code)
            && code.ValueKind == JsonValueKind.Number
            && code.TryGetInt32(out int codeValue)
            && codeValue != 0)
        {
            string? message = json.RootElement.TryGetProperty("message", out JsonElement msg) ? msg.GetString() : null;
            json.Dispose();
            throw new MilvusException(MilvusErrorCode.UnexpectedError,
                $"Bulk import REST call to '{path}' failed with code {codeValue}"
                + (string.IsNullOrEmpty(message) ? "" : $": {message}"));
        }

        int envelopeCode = json.RootElement.TryGetProperty("code", out JsonElement codeElement)
            && codeElement.ValueKind == JsonValueKind.Number && codeElement.TryGetInt32(out int c)
                ? c
                : 0;
        string envelopeMessage = json.RootElement.TryGetProperty("message", out JsonElement messageElement)
            ? messageElement.GetString() ?? ""
            : "";
        JsonElement data = json.RootElement.TryGetProperty("data", out JsonElement dataElement)
            ? dataElement
            : default;

        return new BulkImportResp(json, envelopeCode, envelopeMessage, data);
    }
}
