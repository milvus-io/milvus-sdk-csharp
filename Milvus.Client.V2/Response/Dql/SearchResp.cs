using Milvus.Client.V2.Utils;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Dql;

/// <summary>
/// Represents the result of a search operation.
/// </summary>
public sealed class SearchResp
{
    private SearchResp(
        string collectionName, IReadOnlyList<FieldData> fieldsData, MilvusIds ids,
        long numQueries, IReadOnlyList<float> scores, long limit, IReadOnlyList<long> limits)
    {
        CollectionName = collectionName;
        FieldsData = fieldsData;
        Ids = ids;
        NumQueries = numQueries;
        Scores = scores;
        Limit = limit;
        Limits = limits;
    }

    internal static SearchResp FromGrpc(Grpc.SearchResults response)
        => new(
            response.CollectionName,
            DqlConversions.ProcessReturnedFieldData(response.Results.FieldsData),
            response.Results.Ids is null ? default : MilvusIds.FromGrpc(response.Results.Ids),
            response.Results.NumQueries,
            response.Results.Scores,
            response.Results.TopK,
            response.Results.Topks);

    /// <summary>
    /// The name of the searched collection.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The returned fields data.
    /// </summary>
    public IReadOnlyList<FieldData> FieldsData { get; }

    /// <summary>
    /// The ids of the returned rows.
    /// </summary>
    public MilvusIds Ids { get; }

    /// <summary>
    /// The number of queries executed.
    /// </summary>
    public long NumQueries { get; }

    /// <summary>
    /// The scores of the returned rows.
    /// </summary>
    public IReadOnlyList<float> Scores { get; }

    /// <summary>
    /// The limit used for the search.
    /// </summary>
    public long Limit { get; }

    /// <summary>
    /// The per-query limits.
    /// </summary>
    public IReadOnlyList<long> Limits { get; }
}
