namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates an alias for a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection for which to create the alias.</param>
    /// <param name="alias">The alias to be created.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public Task CreateAliasAsync(
        string collectionName,
        string alias,
        CancellationToken cancellationToken = default)
        => _defaultDatabase.CreateAliasAsync(collectionName, alias, cancellationToken);

    /// <summary>
    /// Drops an alias.
    /// </summary>
    /// <param name="alias">The alias to be dropped.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public Task DropAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _defaultDatabase.DropAliasAsync(alias, cancellationToken);

    /// <summary>
    /// Alters an alias to point to a new collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to which the alias should point.</param>
    /// <param name="alias">The alias to be altered.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public Task AlterAliasAsync(
        string collectionName,
        string alias,
        CancellationToken cancellationToken = default)
        => _defaultDatabase.AlterAliasAsync(collectionName, alias, cancellationToken);
}
