namespace Milvus.Client;

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
    public async Task CreateAliasAsync(
        string collectionName,
        string alias,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);

        var request = new CreateAliasRequest { CollectionName = collectionName, Alias = alias };

        await InvokeAsync(GrpcClient.CreateAliasAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Drops an alias.
    /// </summary>
    /// <param name="alias">The alias to be dropped.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task DropAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(alias);

        var request = new DropAliasRequest { Alias = alias };

        await InvokeAsync(GrpcClient.DropAliasAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Alters an alias to point to a new collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to which the alias should point.</param>
    /// <param name="alias">The alias to be altered.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task AlterAliasAsync(
        string collectionName,
        string alias,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);

        var request = new AlterAliasRequest { CollectionName = collectionName, Alias = alias };

        await InvokeAsync(GrpcClient.AlterAliasAsync, request, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Describes an alias and returns the name of the collection it points to.
    /// </summary>
    /// <param name="alias">The alias to describe.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>The name of the collection that the alias points to.</returns>
    public async Task<string> DescribeAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(alias);

        var request = new DescribeAliasRequest { Alias = alias };

        DescribeAliasResponse response = await InvokeAsync(
            GrpcClient.DescribeAliasAsync, request, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return response.Collection;
    }

    /// <summary>
    /// Lists all aliases in the current database.
    /// </summary>
    /// <param name="collectionName">
    /// Optional collection name to filter aliases. If specified, only returns aliases for this collection.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>A list of alias names.</returns>
    public async Task<IList<string>> ListAliasesAsync(
        string? collectionName = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ListAliasesRequest();

        if (collectionName != null)
        {
            request.CollectionName = collectionName;
        }

        ListAliasesResponse response = await InvokeAsync(
            GrpcClient.ListAliasesAsync, request, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return response.Aliases.ToList();
    }
}
