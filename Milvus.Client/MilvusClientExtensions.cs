namespace Milvus.Client;

/// <summary>
/// Extension methods for <see cref="Client.MilvusClient"/>
/// </summary>
public static class MilvusClientExtensions
{
    /// <summary>
    /// Wrapper methods for <see cref="MilvusClient.GetVersionAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Return <see cref="MilvusVersion"/> instead of <see cref="string"/>.
    /// </remarks>
    /// <param name="milvusClient"></param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>Milvus version</returns>
    public static async Task<MilvusVersion> GetMilvusVersionAsync(
        this MilvusClient milvusClient,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(milvusClient);

        string version = await milvusClient.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        return MilvusVersion.Parse(version);
    }
}
