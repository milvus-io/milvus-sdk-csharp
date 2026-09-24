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
        long numQueries, IReadOnlyList<float> scores, long limit, IReadOnlyList<long> limits,
        FieldData? groupByFieldValue, long? allSearchCount, ulong sessionTs,
        IReadOnlyList<float>? recalls, long cost, long scannedRemoteBytes, long scannedTotalBytes,
        float? cacheHitRatio, IReadOnlyList<SingleResult>? singleResults,
        IReadOnlyList<MilvusHighlightResult>? highlightResults, IReadOnlyList<long>? elementIndices)
    {
        CollectionName = collectionName;
        FieldsData = fieldsData;
        Ids = ids;
        NumQueries = numQueries;
        Scores = scores;
        Limit = limit;
        Limits = limits;
        GroupByFieldValue = groupByFieldValue;
        AllSearchCount = allSearchCount;
        SessionTs = sessionTs;
        Recalls = recalls;
        Cost = cost;
        ScannedRemoteBytes = scannedRemoteBytes;
        ScannedTotalBytes = scannedTotalBytes;
        CacheHitRatio = cacheHitRatio;
        SingleResults = singleResults;
        HighlightResults = highlightResults;
        ElementIndices = elementIndices;
    }

    internal static SearchResp FromGrpc(Grpc.SearchResults response)
    {
        // Results is a singular proto message; guard against a server that omits it.
        Grpc.SearchResultData? results = response.Results;

        long cost = DqlConversions.GetReportValue(response.Status);
        long scannedRemoteBytes = DqlConversions.GetExtraInfoLong(response.Status, "scanned_remote_bytes");
        long scannedTotalBytes = DqlConversions.GetExtraInfoLong(response.Status, "scanned_total_bytes");
        float? cacheHitRatio = DqlConversions.GetExtraInfoFloat(response.Status, "cache_hit_ratio");

        IReadOnlyList<FieldData> fieldsData = results is null
            ? Array.Empty<FieldData>()
            : DqlConversions.ProcessReturnedFieldData(results.FieldsData);
        IReadOnlyList<float> scores = results is null ? Array.Empty<float>() : results.Scores.ToList();
        IReadOnlyList<long> limits = results is null ? Array.Empty<long>() : results.Topks.ToList();

        IReadOnlyList<SingleResult>? singleResults = BuildSingleResults(results, fieldsData, scores, limits);

        return new(
            response.CollectionName,
            fieldsData,
            results?.Ids is null ? default : MilvusIds.FromGrpc(results.Ids),
            results?.NumQueries ?? 0,
            scores,
            results?.TopK ?? 0,
            limits,
            results?.GroupByFieldValue is null
                ? null
                : DqlConversions.ProcessGroupByFieldValue(results.GroupByFieldValue),
            results is null ? null : results.AllSearchCount,
            response.SessionTs,
            results?.Recalls.Count > 0 ? results.Recalls.ToList() : null,
            cost, scannedRemoteBytes, scannedTotalBytes, cacheHitRatio, singleResults,
            results is { HighlightResults.Count: > 0 }
                ? results.HighlightResults.Select(MilvusHighlightResult.FromGrpc).ToList()
                : null,
            results is { ElementIndices.Data.Count: > 0 } ? results.ElementIndices.Data.ToList() : null);
    }

    // Slices the flat N*topk response into one SingleResult per query vector, using the per-query Topks.
    // A missing or empty Topks falls back to the overall TopK for the single-query case.
    private static List<SingleResult>? BuildSingleResults(
        Grpc.SearchResultData? results, IReadOnlyList<FieldData> fieldsData,
        IReadOnlyList<float> scores, IReadOnlyList<long> limits)
    {
        if (results is null || results.NumQueries == 0 || results.NumQueries > int.MaxValue)
        {
            return null;
        }

        // The server normally reports primary_field_name; fall back to "id" (the conventional Milvus primary
        // key name) when it is omitted, matching Java/PyMilvus.
        string pkName = string.IsNullOrEmpty(results.PrimaryFieldName) ? "id" : results.PrimaryFieldName;
        var singleResults = new List<SingleResult>((int)results.NumQueries);
        int offset = 0;
        for (int i = 0; i < results.NumQueries; i++)
        {
            // Per-query topks and the fallback TopK are uint32 on the wire; range-check the long values so an
            // oversized malformed response degrades to an empty per-query result instead of wrapping negative.
            int count = limits.Count > i
                ? limits[i] > int.MaxValue ? 0 : (int)limits[i]
                : results.TopK > int.MaxValue ? 0 : (int)results.TopK;
            if (count <= 0)
            {
                // A query that matched nothing still gets an empty SingleResult so the per-query lists stay
                // index-aligned with NumQueries (and with Recalls/HighlightResults siblings).
                singleResults.Add(new SingleResult(pkName, MilvusIds.FromSlices(null, null), Array.Empty<float>(), Array.Empty<FieldData>()));
                continue;
            }

            IReadOnlyList<long>? longIds = results.Ids?.IntId?.Data is { Count: > 0 } intData
                ? intData.Skip(offset).Take(count).ToList()
                : null;
            IReadOnlyList<string>? stringIds = results.Ids?.StrId?.Data is { Count: > 0 } strData
                ? strData.Skip(offset).Take(count).ToList()
                : null;

            singleResults.Add(new SingleResult(
                pkName,
                MilvusIds.FromSlices(longIds, stringIds),
                scores.Skip(offset).Take(count).ToList(),
                DqlConversions.TakeRows(fieldsData, offset, count)));
            offset += count;
        }

        return singleResults;
    }

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

    /// <summary>
    /// The group-by field value for each returned row, populated when the search used
    /// <see cref="SearchParameters.GroupByField" />.
    /// </summary>
    public FieldData? GroupByFieldValue { get; }

    /// <summary>
    /// The total number of rows matching the search (across all groups), populated when the search
    /// used <see cref="SearchParameters.GroupByField" />. <c>null</c> only when the server returned no
    /// search results at all; otherwise 0 is ambiguous between "matched no rows" and "the server did not
    /// report a count" (the field is a plain proto3 int64 with no presence bit).
    /// </summary>
    public long? AllSearchCount { get; }

    /// <summary>
    /// The server-side timestamp at which the search was executed, used for session-like operations such as
    /// iterators (read-your-writes consistency).
    /// </summary>
    public ulong SessionTs { get; }

    /// <summary>
    /// The recall rates reported by the server, when it reports them (<c>null</c> otherwise). This is a flat
    /// per-returned-row array (length equals the total number of returned hits, not <see cref="NumQueries" />):
    /// the proxy appends one entry per result row, so do not index it by query.
    /// </summary>
    public IReadOnlyList<float>? Recalls { get; }

    /// <summary>
    /// The cost of the search in milliseconds, as reported by the server's <c>report_value</c> extra info,
    /// or 0 when the server does not report a cost.
    /// </summary>
    public long Cost { get; }

    /// <summary>
    /// The number of bytes scanned from remote storage during the search, when reported by the server.
    /// </summary>
    public long ScannedRemoteBytes { get; }

    /// <summary>
    /// The total number of bytes scanned during the search, when reported by the server.
    /// </summary>
    public long ScannedTotalBytes { get; }

    /// <summary>
    /// The cache hit ratio of the search (0–1), when reported by the server. <c>null</c> when not reported.
    /// </summary>
    public float? CacheHitRatio { get; }

    /// <summary>
    /// The per-query-vector results, sliced out of the flat response. One <see cref="SingleResult" /> per query
    /// vector, each holding that query's top-K scores, primary keys and output fields — matching the C++
    /// <c>SearchResults</c> (a vector of <c>SingleResult</c>) and pymilvus <c>Hits</c>. <c>null</c> when the
    /// response carries no result data.
    /// </summary>
    public IReadOnlyList<SingleResult>? SingleResults { get; }

    /// <summary>
    /// The highlight results per field, populated when the search used full-text or hybrid highlighting
    /// (<c>highlightResults</c>), one entry per highlighted field. <c>null</c> when the server reported none.
    /// </summary>
    public IReadOnlyList<MilvusHighlightResult>? HighlightResults { get; }

    /// <summary>
    /// The element indices, populated for matched sub-struct elements when the search operated on
    /// array/struct fields. <c>null</c> when the server reported none.
    /// </summary>
    public IReadOnlyList<long>? ElementIndices { get; }
}
