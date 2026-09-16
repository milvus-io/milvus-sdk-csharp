namespace Milvus.Client;

/// <summary>
/// One matched entity from a <see cref="SearchResults" />, combining its ID, similarity score and
/// requested output fields into a single directly-usable row. See <see cref="SearchResults.GetHits(int)" />.
/// </summary>
public sealed class SearchHit
{
    internal SearchHit(object id, float score, IReadOnlyDictionary<string, object?> fields)
    {
        Id = id;
        Score = score;
        Fields = fields;
    }

    /// <summary>
    /// The primary key of the matched entity: a <see cref="long" /> or a <see cref="string" />,
    /// depending on the collection's primary key type.
    /// </summary>
    public object Id { get; }

    /// <summary>
    /// The similarity score for this hit.
    /// </summary>
    public float Score { get; }

    /// <summary>
    /// The requested output fields for this hit, as specified by <see cref="SearchParameters.OutputFields" />.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Fields { get; }
}
