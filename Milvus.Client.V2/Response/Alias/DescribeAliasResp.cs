namespace Milvus.Client.V2.Responses.Aliases;

/// <summary>
/// Represents the result of a <c>DescribeAlias</c> operation.
/// </summary>
public sealed class DescribeAliasResp
{
    private DescribeAliasResp(string collectionName, string alias, string databaseName)
    {
        CollectionName = collectionName;
        Alias = alias;
        DatabaseName = databaseName;
    }

    internal static DescribeAliasResp FromGrpc(Grpc.DescribeAliasResponse response)
        => new(response.Collection, response.Alias, response.DbName);

    /// <summary>
    /// The name of the collection that the alias points to.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The name of the alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// The database that contains the collection.
    /// </summary>
    public string DatabaseName { get; }
}
