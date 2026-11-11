namespace Milvus.Client.V2.Utils;

/// <summary>
/// Thrown when the global cluster topology API rejects a request, mirroring the Java SDK's
/// <c>GlobalClusterApiException</c>. Authentication failures (HTTP 401/403) are always deterministic; other API
/// errors fail fast only when all reachable seeds agree, while mixed or inconsistent failures are retried by the
/// caller.
/// </summary>
/// <remarks>
/// Zilliz Cloud only. Deliberately internal: the manager never lets it escape to public callers (the connect path
/// converts it to a <see cref="MilvusException" /> and the background refresh logs it), so it carries no
/// serialization story and only the one semantic constructor.
/// </remarks>
#pragma warning disable CA1032 // Internal type with a single semantic constructor; see the remarks above.
#pragma warning disable CA1064 // Deliberately internal; see the remarks above.
internal sealed class GlobalClusterApiException : Exception
{
    internal GlobalClusterApiException(int code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// The HTTP status code (401/403 for authentication failures) or the API-level error code from the
    /// <c>code</c> field of the topology response.
    /// </summary>
    internal int Code { get; }
}
#pragma warning restore CA1064
#pragma warning restore CA1032
