using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

/// <summary>
/// Milvus bulk-import helpers that call the Milvus RESTful import endpoints directly, mirroring the C++
/// <c>milvus::BulkImport</c> and Java <c>BulkImportUtils</c>. Unlike the gRPC operations, these take an
/// explicit <c>url</c> (the Milvus HTTP endpoint) and <c>apiKey</c> (sent verbatim as a
/// <c>Authorization: Bearer &lt;apiKey&gt;</c> header) and return the server's raw JSON payload.
/// </summary>
#pragma warning disable CA1054 // url is a plain string to mirror the C++/Java BulkImport APIs.
public static class MilvusBulkImport
{
    /// <summary>
    /// The <see cref="HttpMessageHandler" /> used for REST calls. Tests inject a handler that captures the
    /// request; the default lazily creates an <see cref="HttpClientHandler" />.
    /// </summary>
    internal static HttpMessageHandler? HttpMessageHandler { get; set; }

    private static HttpClient CreateClient()
        => HttpMessageHandler is null ? new HttpClient() : new HttpClient(HttpMessageHandler, disposeHandler: false);
    /// <summary>
    /// Creates an import job from the given data files.
    /// </summary>
    public static async Task<JsonDocument> CreateImportJobsAsync(
        string url, string collectionName, IEnumerable<string> files,
        string dbName = "default", string? apiKey = null, string? partitionName = null,
        IDictionary<string, string>? options = null, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(url);
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNull(files);

        string[] filesArray = files.ToArray();
        if (filesArray.Length == 0)
        {
            throw new ArgumentException("At least one file must be provided.", nameof(files));
        }

        var payload = new Dictionary<string, object?>
        {
            ["dbName"] = dbName,
            ["collectionName"] = collectionName,
            // The Milvus REST handler binds ImportReq.Files as [][]string (one group per entry), so the files
            // must be wrapped in a single group rather than sent as a flat array, which the server rejects.
            ["files"] = new[] { filesArray }
        };
        if (!string.IsNullOrEmpty(partitionName))
        {
            payload["partitionName"] = partitionName;
        }
        if (options is { Count: > 0 })
        {
            payload["options"] = options;
        }

        return await PostJsonAsync(
                url, "/v2/vectordb/jobs/import/create", payload, dbName, apiKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the import jobs of a collection (or all collections when <paramref name="collectionName" /> is empty).
    /// </summary>
    public static async Task<JsonDocument> ListImportJobsAsync(
        string url, string collectionName, string dbName = "default", string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(url);

        var payload = new Dictionary<string, object?>
        {
            ["collectionName"] = collectionName,
            ["dbName"] = dbName
        };

        return await PostJsonAsync(
                url, "/v2/vectordb/jobs/import/list", payload, dbName, apiKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the progress of an import job.
    /// </summary>
    public static async Task<JsonDocument> GetImportJobProgressAsync(
        string url, string jobId, string dbName = "default", string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(url);
        Verify.NotNullOrWhiteSpace(jobId);

        var payload = new Dictionary<string, object?>
        {
            ["dbName"] = dbName,
            ["jobID"] = jobId
        };

        return await PostJsonAsync(
                url, "/v2/vectordb/jobs/import/get_progress", payload, dbName, apiKey, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<JsonDocument> PostJsonAsync(
        string baseUrl, string path, object payload, string dbName, string? apiKey,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + path);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        // The import progress endpoint binds only the job id from the body and derives the database from the
        // "DB-Name" header (the JobIDReq on the server does not implement DBNameGetter), so always carry the
        // database there too.
        request.Headers.TryAddWithoutValidation("DB-Name", dbName);

        string body;
        try
        {
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
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

        return json;
    }
}
