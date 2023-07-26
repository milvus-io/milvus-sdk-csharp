namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Returns a <see cref="MilvusDatabase" /> representing a Milvus database which can contain collections.
    /// </summary>
    /// <remarks>
    /// Database are available starting Milvus 2.2.9.
    /// </remarks>
    /// <param name="databaseName">The name of the database.</param>
    public MilvusDatabase GetDatabase(string databaseName)
        => new MilvusDatabase(this, databaseName);

    /// <summary>
    /// Creates a new database.
    /// </summary>
    /// <param name="databaseName">The name of the new database to be created.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task<MilvusDatabase> CreateDatabaseAsync(
        string databaseName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(databaseName);

        await InvokeAsync(GrpcClient.CreateDatabaseAsync, new CreateDatabaseRequest
        {
            DbName = databaseName,
        }, cancellationToken).ConfigureAwait(false);

        return new MilvusDatabase(this, databaseName);
    }

    /// <summary>
    /// List all available databases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<string>> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        ListDatabasesResponse response = await InvokeAsync(GrpcClient.ListDatabasesAsync, new ListDatabasesRequest(),
            static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.DbNames;
    }
}
