using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

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
        this.CollectionName = collectionName;
        this.Results = results;
    }
}

/// <summary>
/// Milvus search result data
/// </summary>
public class MilvusSearchResultData
{
    /// <summary>
    /// Fields data
    /// </summary>
    [JsonPropertyName("fields_data")]
    public IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Ids
    /// </summary>
    [JsonPropertyName("ids")]
    public MilvusIds Ids { get; set; }

    /// <summary>
    /// Number of queries
    /// </summary>
    [JsonPropertyName("num_queries")]
    public long NumQueries { get; set; }

    /// <summary>
    /// Scores
    /// </summary>
    [JsonPropertyName("scores")]
    public IList<float> Scores { get; set; }

    /// <summary>
    /// TopK
    /// </summary>
    [JsonPropertyName("top_k")]
    public long TopK { get; set; }

    /// <summary>
    /// TopKs
    /// </summary>
    [JsonPropertyName("topks")]
    public IList<long> TopKs { get; set; }
}