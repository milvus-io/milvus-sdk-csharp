using IO.Milvus.ApiSchema;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Milvus compaction plans.
/// </summary>
public class MilvusCompactionPlans:List<MilvusCompactionPlan>
{
    /// <summary>
    /// Merge infos.
    /// </summary>
    public IList<MilvusCompactionPlan> MergeInfos { get;}

    /// <summary>
    /// State.
    /// </summary>
    public MilvusCompactionState State { get; }

    internal static MilvusCompactionPlans From(
        GetCompactionPlansResponse getCompactionPlansResponse)
    {
        return new MilvusCompactionPlans(getCompactionPlansResponse.MergeInfos, getCompactionPlansResponse.State);
    }

    #region Private =========================================================================================================
    private MilvusCompactionPlans(
    IEnumerable<MilvusCompactionPlan> collection,
    MilvusCompactionState state) : base(collection)
    {
        MergeInfos = collection.ToList();
        State = state;
    }
    #endregion
}

/// <summary>
/// Milvus compaction plan.
/// </summary>
public class MilvusCompactionPlan
{
    /// <summary>
    /// Sources
    /// </summary>
    [JsonPropertyName("sources")]
    public IList<long> Sources { get; }

    /// <summary>
    /// Target
    /// </summary>
    [JsonPropertyName("target")]
    public long Target { get; }
}
