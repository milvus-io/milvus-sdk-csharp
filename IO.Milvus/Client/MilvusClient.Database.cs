using IO.Milvus.Grpc;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates a new database.
    /// </summary>
    /// <param name="dbName">The name of the new database to be created.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreateDatabaseAsync, new CreateDatabaseRequest
        {
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
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
        ListDatabasesResponse response = await InvokeAsync(_grpcClient.ListDatabasesAsync, new ListDatabasesRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.DbNames;
    }

    /// <summary>
    /// Drops a database.
    /// </summary>
    /// <param name="dbName">The name of the database to be dropped.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task DropDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropDatabaseAsync, new DropDatabaseRequest
        {
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
    }
}
