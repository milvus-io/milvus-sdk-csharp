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
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);

        var request = new CreateAliasRequest { CollectionName = collectionName, Alias = alias };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.CreateAliasAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete an Alias
    /// </summary>
    /// <param name="alias">Alias</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DropAliasAsync(
        string alias,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(alias);

        var request = new DropAliasRequest { Alias = alias };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.DropAliasAsync, request, cancellationToken).ConfigureAwait(false);
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
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);

        var request = new AlterAliasRequest { CollectionName = collectionName, Alias = alias };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.AlterAliasAsync, request, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
