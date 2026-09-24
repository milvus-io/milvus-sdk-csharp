namespace Milvus.Client.V2.Responses.Database;

/// <summary>
/// The result of a list-databases request.
/// </summary>
public sealed class ListDatabasesResp
{
    private ListDatabasesResp(
        IReadOnlyList<string> databaseNames, IReadOnlyList<ulong> createdTimestamps, IReadOnlyList<long> databaseIds)
    {
        DatabaseNames = databaseNames;
        CreatedTimestamps = createdTimestamps;
        DatabaseIds = databaseIds;
    }

    internal static ListDatabasesResp FromGrpc(Grpc.ListDatabasesResponse response)
        => new(response.DbNames.ToList(), response.CreatedTimestamp.ToList(), response.DbIds.ToList());

    /// <summary>
    /// The names of the databases in the cluster.
    /// </summary>
    public IReadOnlyList<string> DatabaseNames { get; }

    /// <summary>
    /// The hybrid timestamps at which the databases were created, aligned with <see cref="DatabaseNames" />.
    /// </summary>
    public IReadOnlyList<ulong> CreatedTimestamps { get; }

    /// <summary>
    /// The ids of the databases, aligned with <see cref="DatabaseNames" />.
    /// </summary>
    public IReadOnlyList<long> DatabaseIds { get; }
}
