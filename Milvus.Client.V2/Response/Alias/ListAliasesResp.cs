namespace Milvus.Client.V2.Responses.Aliases;

/// <summary>
/// Represents the result of a <c>ListAliases</c> operation.
/// </summary>
public sealed class ListAliasesResp
{
    private ListAliasesResp(string collectionName, string databaseName, IReadOnlyList<string> aliases)
    {
        CollectionName = collectionName;
        DatabaseName = databaseName;
        Aliases = aliases;
    }

    internal static ListAliasesResp FromGrpc(Grpc.ListAliasesResponse response)
        => new(response.CollectionName, response.DbName, response.Aliases.ToList());

    /// <summary>
    /// The name of the collection whose aliases are listed.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The database that contains the collection.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// The aliases of the requested collection or collections.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }
}
