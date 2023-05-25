using Google.Protobuf.Collections;
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
        return new MilvusFlushResult(response.CollSegIDs, response.FlushCollSegIDs, response.CollSealTimes);
    }

    #region Private ==========================================================================================
    private MilvusFlushResult(
        MapField<string, LongArray> collSegIDs,
        MapField<string, LongArray> flushCollSegIDs, 
        MapField<string, long> collSealTimes)
    {
        this.CollSegIDs = collSegIDs.ToDictionary(p => p.Key ,p => new MilvusId<long>() { Data = p.Value.Data});
        this.FlushCollSegIds = flushCollSegIDs.ToDictionary(p => p.Key, p => new MilvusId<long>() { Data = p.Value.Data });
        this.CollSealTimes = collSealTimes;
    }
    #endregion
}
