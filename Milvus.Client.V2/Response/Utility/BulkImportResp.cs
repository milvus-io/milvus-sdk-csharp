using System.Text.Json;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The envelope returned by the Milvus bulk-import REST endpoints: the server <c>code</c>/<c>message</c> plus
/// the endpoint-specific <c>data</c> payload. Mirrors the Rust SDK's <c>BulkImportResponse</c>.
/// </summary>
public sealed class BulkImportResp : IDisposable
{
    private readonly JsonDocument _document;
    private readonly JsonDocument? _dataDocument;

    internal BulkImportResp(JsonDocument document, int code, string message, JsonElement data)
    {
        _document = document;
        Code = code;
        Message = message;
        // Snapshot the data payload into its own document so Data and the accessors (JobId/State/Progress/...)
        // remain readable after Dispose() instead of throwing ObjectDisposedException on the envelope document.
        _dataDocument = data.ValueKind == JsonValueKind.Object
            ? JsonDocument.Parse(data.GetRawText())
            : null;
        Data = _dataDocument?.RootElement ?? default;
    }

    /// <summary>
    /// The server response code (0 on success).
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// The server response message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The endpoint-specific response payload.
    /// </summary>
    public JsonElement Data { get; }

    /// <summary>
    /// The created or described import job id, when supplied in <see cref="Data" />.
    /// </summary>
    public string? JobId => GetString("jobId");

    /// <summary>
    /// The import state, when supplied by the describe/progress endpoint.
    /// </summary>
    public string? State => GetString("state");

    /// <summary>
    /// The import progress percentage, when supplied by the describe/progress endpoint.
    /// </summary>
    public long? Progress => GetInt64("progress");

    /// <summary>
    /// The import failure reason, when supplied by the describe/progress endpoint.
    /// </summary>
    public string? Reason => GetString("reason");

    /// <summary>
    /// Returns one field from the endpoint-specific <see cref="Data" /> payload.
    /// </summary>
    public JsonElement GetDataField(string name)
        => Data.TryGetProperty(name, out JsonElement element) ? element : default;

    /// <inheritdoc />
    /// <remarks>
    /// Disposes only the outer envelope document; <see cref="Data" /> is a snapshot owned separately and stays
    /// readable after this call.
    /// </remarks>
    public void Dispose() => _document.Dispose();

    private string? GetString(string name)
        => GetDataField(name) is { ValueKind: JsonValueKind.String } element ? element.GetString() : null;

    private long? GetInt64(string name)
        => GetDataField(name) is { ValueKind: JsonValueKind.Number } element && element.TryGetInt64(out long value)
            ? value
            : null;
}
