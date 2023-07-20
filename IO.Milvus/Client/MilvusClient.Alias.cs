using IO.Milvus.Grpc;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates an alias for a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection for which to create the alias.</param>
    /// <param name="alias">The alias to be created.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Drops an alias.
    /// </summary>
    /// <param name="alias">The alias to be dropped.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Alters an alias to point to a new collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to which the alias should point.</param>
    /// <param name="alias">The alias to be altered.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
