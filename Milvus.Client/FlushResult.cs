namespace Milvus.Client;

/// <summary>
/// Milvus flush result.
/// </summary>
public sealed class FlushResult
{
    /// <summary>
    /// Collection segment ids.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A segment is a data file automatically created by Milvus for holding inserted data.
    /// </para>
    /// <para>
    /// A collection can have multiple segments and a segment can have multiple entities.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> CollSegIDs { get; }

    /// <summary>
    /// Flush collection segment ids.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> FlushCollSegIds { get; }

    /// <summary>
    /// Collection seal times.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A segment can be either growing or sealed. A growing segment keeps receiving the newly inserted data till it is sealed.
    /// </para>
    /// <para>
    /// A sealed segment no longer receives any new data, and will be flushed to the object storage
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, long> CollSealTimes { get; }

    internal static FlushResult From(FlushResponse response)
        => new(
            response.CollSegIDs.ToDictionary(static p => p.Key,
                static p => (IReadOnlyList<long>)p.Value.Data.ToArray()),
            response.FlushCollSegIDs.ToDictionary(static p => p.Key,
                static p => (IReadOnlyList<long>)p.Value.Data.ToArray()),
            response.CollSealTimes);

    private FlushResult(
        IReadOnlyDictionary<string, IReadOnlyList<long>> collSegIDs,
        IReadOnlyDictionary<string, IReadOnlyList<long>> flushCollSegIDs,
        IReadOnlyDictionary<string, long> collSealTimes)
    {
        CollSegIDs = collSegIDs;
        FlushCollSegIds = flushCollSegIDs;
        CollSealTimes = collSealTimes;
    }
}
