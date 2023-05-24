using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// milvus flush result.
/// </summary>
public class MilvusFlushResult
{
    /// <summary>
    /// Coll segIDs.
    /// </summary>
    public IDictionary<string, MilvusId<long>> CollSegIDs { get; set; }

    /// <summary>
    /// FlushCollSegIds.
    /// </summary>
    public IDictionary<string, MilvusId<long>> FlushCollSegIds { get; set; }

    /// <summary>
    /// CollSealTimes.
    /// </summary>
    public IDictionary<string, long> CollSealTimes { get; set; }
}
