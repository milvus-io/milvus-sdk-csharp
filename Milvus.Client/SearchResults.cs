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
}
