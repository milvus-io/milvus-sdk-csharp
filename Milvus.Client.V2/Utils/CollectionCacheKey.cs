namespace Milvus.Client.V2.Utils;

/// <summary>
/// Immutable key that uniquely identifies a collection for the schema / timestamp caches, composed of the
/// normalized endpoint, database name and collection name.
/// </summary>
internal readonly record struct CollectionCacheKey(string Endpoint, string Database, string Collection)
{
    /// <summary>
    /// Creates a cache key, normalizing the endpoint (lower-case host:port) and defaulting an empty database
    /// to <c>"default"</c>.
    /// </summary>
    public static CollectionCacheKey Create(string endpoint, string database, string collection)
        => new(
            NormalizeEndpoint(endpoint),
            string.IsNullOrEmpty(database) ? "default" : database,
            collection ?? "");

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "";
        }

        string value = endpoint.Trim();
        string uriValue = value.Contains("://", StringComparison.Ordinal) ? value : $"http://{value}";

        if (!Uri.TryCreate(uriValue, UriKind.Absolute, out Uri? uri) || uri.Host.Length == 0)
        {
            return value;
        }

        // Uri.Port returns the scheme default (80/443) when the port is omitted; normalize to Milvus's
        // default (19530) and lower-case the host so equivalent URIs produce the same cache key.
        int port = uri.IsDefaultPort ? 19530 : uri.Port;
#pragma warning disable CA1308 // The cache key contract is documented as lower-case host:port; either casing is unambiguous as a key.
        string host = uri.Host.Contains(':') ? $"[{uri.Host.ToLowerInvariant()}]" : uri.Host.ToLowerInvariant();
#pragma warning restore CA1308
        return $"{host}:{port}";
    }
}
