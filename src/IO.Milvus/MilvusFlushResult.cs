using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// milvus flush result.
/// </summary>
public class MilvusFlushResult
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
            response.CollSegIDs.ToDictionary(p => p.Key, p => new MilvusId<long>() { Data = p.Value.Data }),
            response.FlushCollSegIDs.ToDictionary(p => p.Key, p => new MilvusId<long>() { Data = p.Value.Data }),
            response.CollSealTimes);
    }

    internal static MilvusFlushResult From(ApiSchema.FlushResponse data)
    {
        return new MilvusFlushResult(
            data.CollSegIDs.ToDictionary(p => p.Key, p => p.Value),
            data.FlushCollSegIds,
            data.CollSealTimes);
    }

    #region Private ==========================================================================================
    private MilvusFlushResult(
        IDictionary<string, MilvusId<long>> collSegIDs,
        IDictionary<string, MilvusId<long>> flushCollSegIDs,
        IDictionary<string, long> collSealTimes)
    {
        this.CollSegIDs = collSegIDs;
        this.FlushCollSegIds = flushCollSegIDs;
        this.CollSealTimes = collSealTimes;
    }
    #endregion
}
