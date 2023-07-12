using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Milvus Search Result.
/// </summary>
public sealed class MilvusSearchResult
{
    /// <summary>
    /// Collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// Results.
    /// </summary>
    public MilvusSearchResultData Results { get; }

    internal static MilvusSearchResult From(Grpc.SearchResults searchResults)
    {
        return new MilvusSearchResult(
            searchResults.CollectionName,
            Converter(searchResults.Results));
    }

    private static MilvusSearchResultData Converter(Grpc.SearchResultData results)
    {
        return new MilvusSearchResultData()
        {
            FieldsData = results.FieldsData.Select(Field.FromGrpcFieldData).ToList(),
            Ids = MilvusIds.From(results.Ids),
            NumQueries = results.NumQueries,
            Scores = results.Scores,
            TopK = results.TopK,
            TopKs = results.Topks,
        };
    }

    private MilvusSearchResult(string collectionName, MilvusSearchResultData results)
    {
        CollectionName = collectionName;
        Results = results;
    }
}