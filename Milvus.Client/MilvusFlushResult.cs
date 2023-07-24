namespace Milvus.Client;

/// <summary>
/// milvus flush result.
/// </summary>
public sealed class MilvusFlushResult
{
    /// <summary>
    /// Coll segIDs.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> CollSegIDs { get; }

    /// <summary>
    /// FlushCollSegIds.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> FlushCollSegIds { get; }

    /// <summary>
    /// CollSealTimes.
    /// </summary>
    public IReadOnlyDictionary<string, long> CollSealTimes { get; }

    internal static MilvusFlushResult From(FlushResponse response)
        => new(
            response.CollSegIDs.ToDictionary(static p => p.Key,
                static p => (IReadOnlyList<long>)p.Value.Data.ToArray()),
            response.FlushCollSegIDs.ToDictionary(static p => p.Key,
                static p => (IReadOnlyList<long>)p.Value.Data.ToArray()),
            response.CollSealTimes);

    private MilvusFlushResult(
        IReadOnlyDictionary<string, IReadOnlyList<long>> collSegIDs,
        IReadOnlyDictionary<string, IReadOnlyList<long>> flushCollSegIDs,
        IReadOnlyDictionary<string, long> collSealTimes)
    {
        CollSegIDs = collSegIDs;
        FlushCollSegIds = flushCollSegIDs;
        CollSealTimes = collSealTimes;
    }
}
