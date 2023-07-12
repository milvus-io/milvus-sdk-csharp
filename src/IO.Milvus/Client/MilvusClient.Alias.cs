using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

/// <summary>
/// Milvus client
/// </summary>
public partial class MilvusClient
{
    /// <summary>
    /// Create an alias for a collection name.
    /// </summary>
    /// <param name="collectionName">Collection Name.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CreateAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreateAliasAsync, new CreateAliasRequest
        {
            CollectionName = collectionName,
            Alias = alias,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete an Alias
    /// </summary>
    /// <param name="alias">Alias</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DropAliasAsync(
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropAliasAsync, new DropAliasRequest
        {
            Alias = alias,
            DbName = dbName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Alter an alias
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="alias">Alias</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AlterAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.AlterAliasAsync, new AlterAliasRequest
        {
            CollectionName = collectionName,
            Alias = alias,
            DbName = dbName
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}