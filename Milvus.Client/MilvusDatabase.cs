namespace Milvus.Client;

/// <summary>
/// Represents a Milvus database which can contain collections.
/// </summary>
/// <remarks>
/// Database are available starting Milvus 2.2.9.
/// </remarks>
public partial class MilvusDatabase
{
    private readonly MilvusClient _client;

    /// <summary>
    /// The name of this database.
    /// </summary>
    public string? Name { get; }

    internal MilvusDatabase(MilvusClient client, string? databaseName)
        => (_client, Name) = (client, databaseName);

    /// <summary>
    /// Returns a <see cref="MilvusCollection" /> representing a Milvus collection in this database. This is the
    /// starting point for all on which all collection operations.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    public MilvusCollection GetCollection(string collectionName)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        return new MilvusCollection(_client, collectionName, Name);
    }

    /// <summary>
    /// Drops the database.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task DropAsync(CancellationToken cancellationToken = default)
    {
        await _client.InvokeAsync(_client.GrpcClient.DropDatabaseAsync, new DropDatabaseRequest
        {
            DbName = Name,
        }, cancellationToken).ConfigureAwait(false);
    }
}
