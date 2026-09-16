namespace Milvus.Client;

/// <summary>
/// The results from a vector similarity search executed via <see cref="MilvusCollection.SearchAsync{T}(string, IReadOnlyList{ReadOnlyMemory{T}}, SimilarityMetricType, int, SearchParameters, CancellationToken)" />.
/// </summary>
public sealed class SearchResults
{
    /// <summary>
    /// The name of the searched collection.
    /// </summary>
    public required string CollectionName { get; init; }

    /// <summary>
    /// The fields returned from the search, as specified by <see cref="SearchParameters.OutputFields" />.
    /// </summary>
    public required IReadOnlyList<FieldData> FieldsData { get; init; }

    /// <summary>
    /// The IDs of the rows returned from the search.
    /// </summary>
    public required MilvusIds Ids { get; init; }

    /// <summary>
    /// The number of queries executed.
    /// </summary>
    public required long NumQueries { get; init; }

    /// <summary>
    /// The scores for the results.
    /// </summary>
    public required IReadOnlyList<float> Scores { get; init; }

#pragma warning disable CS1591
    public required long Limit { get; init; }

    public required IReadOnlyList<long> Limits { get; init; }
#pragma warning restore CS1591

    /// <summary>
    /// Combines <see cref="Ids" />, <see cref="Scores" /> and <see cref="FieldsData" /> into one
    /// <see cref="SearchHit" /> per matched entity for a single query vector -- a directly-usable row,
    /// rather than three parallel column-oriented arrays the caller has to zip together by hand.
    /// </summary>
    /// <param name="queryIndex">
    /// Which query vector's hits to return. A search can run against several query vectors at once
    /// (<see cref="NumQueries" />), and every array on this type is a flat concatenation of each
    /// query's hits in order; this slices out just one query's share, using <see cref="Limits" />.
    /// Defaults to 0, the common case of searching with a single query vector.
    /// </param>
    public IReadOnlyList<SearchHit> GetHits(int queryIndex = 0)
    {
        if (queryIndex < 0 || queryIndex >= Limits.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(queryIndex), queryIndex,
                $"Must be in [0, {Limits.Count}) -- this result has {NumQueries} quer" +
                (NumQueries == 1 ? "y" : "ies") + ".");
        }

        int start = 0;
        checked
        {
            try
            {
                for (int i = 0; i < queryIndex; i++)
                {
                    start += (int)Limits[i];
                }
            }
            catch (OverflowException exception)
            {
                throw new MilvusException(
                    $"This result's {nameof(Limits)} overflow a 32-bit row offset before reaching query " +
                    $"{queryIndex}; the server-reported hit counts are too large for {nameof(GetHits)} to slice.",
                    exception);
            }
        }

        int count = checked((int)Limits[queryIndex]);

        int idCount = Ids.LongIds?.Count ?? Ids.StringIds?.Count ?? 0;
        if (start + count > idCount || start + count > Scores.Count)
        {
            throw new MilvusException(
                $"Query {queryIndex}'s slice [{start}, {start + count}) does not fit within the " +
                $"{idCount} id(s) and {Scores.Count} score(s) this result actually carries -- the server " +
                $"reported inconsistent {nameof(Limits)} for this search.");
        }

        // Only this query's slice of FieldsData needs pivoting, not the whole (potentially
        // multi-query) result -- PivotToRows(start, count) avoids re-pivoting every other query's
        // rows on every call.
        List<Dictionary<string, object?>> rows = MilvusCollection.PivotToRows(FieldsData, start, count);

        List<SearchHit> hits = new(count);
        for (int i = 0; i < count; i++)
        {
            object id = Ids.LongIds is not null ? Ids.LongIds[start + i] : Ids.StringIds![start + i];
            IReadOnlyDictionary<string, object?> fields = i < rows.Count ? rows[i] : new Dictionary<string, object?>();
            hits.Add(new SearchHit(id, Scores[start + i], fields));
        }

        return hits;
    }
}
