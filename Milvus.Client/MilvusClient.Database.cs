namespace Milvus.Client;

public partial class MilvusClient
{
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
    public async Task CreateDatabaseAsync(
        string databaseName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(databaseName);

        await InvokeAsync(
                GrpcClient.CreateDatabaseAsync, new CreateDatabaseRequest { DbName = databaseName }, cancellationToken)
            .ConfigureAwait(false);
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
        ListDatabasesResponse response = await InvokeAsync(
                GrpcClient.ListDatabasesAsync, new ListDatabasesRequest(), static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return response.DbNames;
    }

    /// <summary>
    /// Drops a database.
    /// </summary>
    /// <param name="databaseName">The name of the database to be dropped.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Available starting Milvus 2.2.9.
    /// </para>
    /// </remarks>
    public async Task DropDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        await InvokeAsync(
                GrpcClient.DropDatabaseAsync, new DropDatabaseRequest { DbName = databaseName }, cancellationToken)
            .ConfigureAwait(false);
    }
}
