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
        // string.Contains(string) is available on netstandard2.0/net462 (the char overload is not, so
        // CA1847's suggestion is intentionally not followed for the multi-target build). CA1307's
        // StringComparison overload likewise does not exist on netstandard2.0/net462.
#pragma warning disable CA1307, CA1847
        string uriValue = value.Contains("://") ? value : $"http://{value}";
#pragma warning restore CA1307, CA1847

        if (!Uri.TryCreate(uriValue, UriKind.Absolute, out Uri? uri) || uri.Host.Length == 0)
        {
            return value;
        }

        // Uri.Port returns the scheme default (80/443) when the port is omitted; normalize to Milvus's
        // default (19530) and lower-case the host so equivalent URIs produce the same cache key. DnsSafeHost
        // is always unbracketed (on .NET 8+ Uri.Host already returns "[::1]" for an IPv6 literal, while
        // netstandard2.0/net462 return "::1"), so brackets are added only when the host contains ':' to
        // keep the key identical across target frameworks.
        int port = uri.IsDefaultPort ? 19530 : uri.Port;
#pragma warning disable CA1308, CA1847 // The cache key contract is documented as lower-case host:port; the string overload is required for netstandard2.0/net462.
        string host = uri.DnsSafeHost.ToLowerInvariant();
#pragma warning disable CA1307 // string.Contains(string) has no StringComparison overload on netstandard2.0/net462.
        if (host.Contains(":"))
#pragma warning restore CA1307
        {
            host = $"[{host}]";
        }
#pragma warning restore CA1308, CA1847
        return $"{host}:{port}";
    }
}
