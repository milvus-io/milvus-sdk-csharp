namespace Milvus.Examples;

/// <summary>
/// Shared helpers for the examples.
/// </summary>
internal static class ExampleHelpers
{
    /// <summary>
    /// Creates a <see cref="Milvus.Client.V2.MilvusClientV2" /> from a "host:port" URI, using the token
    /// (username:password) from <paramref name="defaultToken" /> when <c>MILVUS_TOKEN</c> is not set.
    /// </summary>
    public static Milvus.Client.V2.MilvusClientV2 CreateClient(string uri, string? defaultToken = null)
    {
        string? token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? defaultToken;

        var config = new Milvus.Client.V2.Types.ConnectConfig { Uri = uri };

        if (token is not null)
        {
            int colon = token.IndexOf(':');
            if (colon > 0)
            {
                config.Username = token[..colon];
                config.Password = token[(colon + 1)..];
            }
            else
            {
                config.ApiKey = token;
            }
        }

        var client = new Milvus.Client.V2.MilvusClientV2(config);
        return client;
    }

    /// <summary>
    /// Drops a collection if it exists, so examples are idempotent.
    /// </summary>
    public static async Task ResetCollectionAsync(
        Milvus.Client.V2.MilvusClientV2 client, string collectionName, CancellationToken ct = default)
    {
        if ((await client.HasCollectionAsync(new Milvus.Client.V2.Requests.Collection.HasCollectionReq
        {
            CollectionName = collectionName
        }, ct)).Has)
        {
            await client.DropCollectionAsync(new Milvus.Client.V2.Requests.Collection.DropCollectionReq
            {
                CollectionName = collectionName
            }, ct);
        }
    }
}
