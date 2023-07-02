using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Milvus Search Result.
/// </summary>
public class MilvusSearchResult
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

    private static MilvusSearchResultData Converter(SearchResultData results)
    {
        return new MilvusSearchResultData()
        {
            FieldsData = results.FieldsData.Select(f => Field.FromGrpcFieldData(f)).ToList(),
            Ids = MilvusIds.From(results.Ids),
            NumQueries = results.NumQueries,
            Scores = results.Scores,
            TopK = results.TopK,
            TopKs = results.Topks,
        };
    }

    internal static MilvusSearchResult From(SearchResponse searchResponse)
    {
        return new MilvusSearchResult(searchResponse.CollectionName, searchResponse.Results);
    }

    private MilvusSearchResult(string collectionName, MilvusSearchResultData results)
    {
        CollectionName = collectionName;
        Results = results;
    }
}