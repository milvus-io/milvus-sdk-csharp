using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// milvus flush result.
/// </summary>
public sealed class MilvusFlushResult
{
    /// <summary>
    /// Coll segIDs.
    /// </summary>
    public IDictionary<string, MilvusId<long>> CollSegIDs { get; }

    /// <summary>
    /// FlushCollSegIds.
    /// </summary>
    public IDictionary<string, MilvusId<long>> FlushCollSegIds { get; }

    /// <summary>
    /// CollSealTimes.
    /// </summary>
    public IDictionary<string, long> CollSealTimes { get; }

    internal static MilvusFlushResult From(FlushResponse response)
    {
        return new MilvusFlushResult(
            response.CollSegIDs.ToDictionary(static p => p.Key, static p => new MilvusId<long>(p.Value.Data)),
            response.FlushCollSegIDs.ToDictionary(static p => p.Key, static p => new MilvusId<long>(p.Value.Data)),
            response.CollSealTimes);
    }

    #region Private ==========================================================================================
    private MilvusFlushResult(
        IDictionary<string, MilvusId<long>> collSegIDs,
        IDictionary<string, MilvusId<long>> flushCollSegIDs,
        IDictionary<string, long> collSealTimes)
    {
        CollSegIDs = collSegIDs;
        FlushCollSegIds = flushCollSegIDs;
        CollSealTimes = collSealTimes;
    }
    #endregion
}
