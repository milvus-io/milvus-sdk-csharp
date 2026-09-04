using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Highlight fragments for one field of a full-text/hybrid search result, mapped from the proto
/// <c>common.HighlightResult</c>. The <see cref="Datas" /> list is flat (one entry per hit, in the same order
/// as the hits flattened over all queries); each <see cref="MilvusHighlightData" /> is the highlight for a
/// single hit.
/// </summary>
public sealed class MilvusHighlightResult
{
    internal MilvusHighlightResult(string fieldName, IReadOnlyList<MilvusHighlightData> datas)
    {
        FieldName = fieldName;
        Datas = datas;
    }

    /// <summary>
    /// The name of the highlighted field.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The per-hit highlight data, flattened across all queries in the same order as the search hits.
    /// </summary>
    public IReadOnlyList<MilvusHighlightData> Datas { get; }

    internal static MilvusHighlightResult FromGrpc(Grpc.HighlightResult grpc)
        => new(
            grpc.FieldName,
            grpc.Datas.Select(d => new MilvusHighlightData(
                d.Fragments.ToList(),
                d.Scores.ToList())).ToList());
}

/// <summary>
/// The highlight fragments and scores for a single query of a highlighted field.
/// </summary>
public sealed class MilvusHighlightData
{
    internal MilvusHighlightData(IReadOnlyList<string> fragments, IReadOnlyList<float> scores)
    {
        Fragments = fragments;
        Scores = scores;
    }

    /// <summary>
    /// The matched fragments.
    /// </summary>
    public IReadOnlyList<string> Fragments { get; }

    /// <summary>
    /// The per-fragment relevance scores.
    /// </summary>
    public IReadOnlyList<float> Scores { get; }
}
