namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a flush operation.
/// </summary>
public sealed class FlushResp
{
    internal FlushResp(
        string databaseName,
        IReadOnlyDictionary<string, IReadOnlyList<long>> collSegIDs,
        IReadOnlyDictionary<string, IReadOnlyList<long>> flushCollSegIDs,
        IReadOnlyDictionary<string, long> collSealTimes,
        IReadOnlyDictionary<string, ulong> collFlushTs)
    {
        DatabaseName = databaseName;
        CollSegIDs = collSegIDs;
        FlushCollSegIDs = flushCollSegIDs;
        CollSealTimes = collSealTimes;
        CollFlushTs = collFlushTs;
    }

    internal static FlushResp FromGrpc(Grpc.FlushResponse response)
        => new(
            response.DbName,
            response.CollSegIDs.ToDictionary(e => e.Key, e => (IReadOnlyList<long>)e.Value.Data.ToList()),
            response.FlushCollSegIDs.ToDictionary(e => e.Key, e => (IReadOnlyList<long>)e.Value.Data.ToList()),
            response.CollSealTimes,
            response.CollFlushTs);

    /// <summary>
    /// The database that contains the flushed collections.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// The segment IDs per collection.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> CollSegIDs { get; }

    /// <summary>
    /// The set of segments actually flushed by this flush, per collection.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> FlushCollSegIDs { get; }

    /// <summary>
    /// The physical seal time per collection, used by backup tooling.
    /// </summary>
    public IReadOnlyDictionary<string, long> CollSealTimes { get; }

    /// <summary>
    /// The hybrid timestamp per collection, used to poll the flush state.
    /// </summary>
    public IReadOnlyDictionary<string, ulong> CollFlushTs { get; }
}
