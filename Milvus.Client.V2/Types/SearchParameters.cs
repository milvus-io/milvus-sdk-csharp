using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A set of optional parameters for performing a search.
/// </summary>
public sealed class SearchParameters
{
    internal List<string>? OutputFieldsInternal { get; private set; }
    internal List<string>? PartitionNamesInternal { get; private set; }

    /// <summary>
    /// Number of entities to skip during the search. If set, the sum of this parameter and of the search
    /// <c>limit</c> must be between 1 and 16384.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// The consistency level to be used in the search. Defaults to the consistency level configured for the
    /// collection.
    /// </summary>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// If set, guarantee that the search is performed after any updates up to the provided timestamp.
    /// </summary>
    public ulong? GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Specifies an optional time travel timestamp; the search will get results based on the data at that point
    /// in time.
    /// </summary>
    public ulong? TimeTravelTimestamp { get; set; }

    /// <summary>
    /// A boolean expression to filter the search results.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// The number of decimal places to round the scores to.
    /// </summary>
    /// <remarks>
    /// Must be between -1 and 6: <c>-1</c> (the server default) disables rounding, <c>0</c> rounds to integer,
    /// and positive values round to that many decimals. The server rejects values outside this range.
    /// </remarks>
    public long? RoundDecimal { get; set; }

    /// <summary>
    /// The HNSW index's candidate list size (<c>ef</c>) for the search, trading recall against speed. Sent as
    /// the <c>"ef"</c> search param, matching pymilvus's <c>search_params={"ef": ...}</c>.
    /// </summary>
    public int? Ef { get; set; }

    /// <summary>
    /// The number of IVF clusters to probe during the search. Sent as the <c>"nprobe"</c> search param, matching
    /// pymilvus's <c>search_params={"nprobe": ...}</c>.
    /// </summary>
    public int? Nprobe { get; set; }

    /// <summary>
    /// The fraction of results to drop for the search (used by hybrid/rerank-aware paths), a value in [0, 1).
    /// Sent as the <c>"drop_ratio_search"</c> search param.
    /// </summary>
    public double? DropRatioSearch { get; set; }

    /// <summary>
    /// The analyzer name to use for BM25/analyzer-based text searches. Sent as the <c>"analyzer_name"</c>
    /// search param.
    /// </summary>
    public string? AnalyzerName { get; set; }

    /// <summary>
    /// The hints for the search (e.g. <c>"L2HINT:True"</c> to run on second-tier resources). Sent as the
    /// <c>"hints"</c> search param.
    /// </summary>
    public string? Hints { get; set; }

    /// <summary>
    /// The JSON path used to extract and search a value inside a JSON field. Sent as the <c>"json_path"</c>
    /// search param.
    /// </summary>
    public string? JsonPath { get; set; }

    /// <summary>
    /// The JSON type to cast the JSON field value to before matching: one of <c>"Int8"</c>, <c>"Int16"</c>,
    /// <c>"Int32"</c>, <c>"Int64"</c>, <c>"Bool"</c> or <c>"VarChar"</c>. Sent as the <c>"json_type"</c> search
    /// param, mirroring pymilvus's json_type validation.
    /// </summary>
    public string? JsonType { get; set; }

    /// <summary>
    /// Whether to preserve the JSON array element order when retrieving elements by <see cref="JsonPath" />.
    /// Sent as the <c>"page_retain_order"</c> search param, mirroring pymilvus's boolean check.
    /// </summary>
    public bool? PageRetainOrder { get; set; }

    /// <summary>
    /// Additional search parameters passed through to the server.
    /// </summary>
    public IDictionary<string, string> ExtraParameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to ignore the growing segments during the search.
    /// </summary>
    public bool? IgnoreGrowing { get; set; }

    /// <summary>
    /// The field to group the search results by.
    /// </summary>
    public string? GroupByField { get; set; }

    /// <summary>
    /// The ordered list of fields to sort the search results by, carried on the wire <c>order_by_fields</c> key.
    /// </summary>
    public IList<OrderByField> OrderByFields { get; } = new List<OrderByField>();

    /// <summary>
    /// The maximum number of results per group when grouping is used.
    /// </summary>
    public int? GroupSize { get; set; }

    /// <summary>
    /// Whether the group size is strict.
    /// </summary>
    public bool? StrictGroupSize { get; set; }

    /// <summary>
    /// The names of fields to be returned from the search.
    /// </summary>
    public IList<string> OutputFields => OutputFieldsInternal ??= new();

    /// <summary>
    /// An optional list of partitions to be searched in the collection.
    /// </summary>
    public IList<string> PartitionNames => PartitionNamesInternal ??= new();

    /// <summary>
    /// The timezone used for the search, e.g. <c>"+08:00"</c> or <c>"America/New_York"</c>.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// The search radius for range searches on binary/float vectors.
    /// </summary>
    /// <remarks>
    /// Used together with <see cref="RangeFilter" /> to bound the distance/similarity of the returned hits
    /// (e.g. <c>"0.6"</c>); the combination is only honored by range-search-enabled index types. The value is
    /// sent as a number inside the search params, so pass a plain numeric string (e.g. <c>Radius = "0.6"</c>).
    /// </remarks>
    public string? Radius { get; set; }

    /// <summary>
    /// The range filter for range searches on binary/float vectors.
    /// </summary>
    /// <remarks>
    /// Combined with <see cref="Radius" /> to define the accepted distance interval (e.g. <c>"0.2"</c>);
    /// range-search-enabled index types are required. The value is sent as a number inside the search params,
    /// so pass a plain numeric string (e.g. <c>RangeFilter = "0.2"</c>).
    /// </remarks>
    public string? RangeFilter { get; set; }

    /// <summary>
    /// The reranker to apply to the search results (e.g. <c>"rrf"</c>).
    /// </summary>
    /// <remarks>
    /// The Milvus 2.6 proxy never reads a <c>"rerank"</c> search-param key, so setting this property causes the
    /// request conversion to throw <see cref="ArgumentException" /> when the search is issued. Use a
    /// function-score reranker via <see cref="FunctionScoreReranker" />, or for hybrid search use an
    /// <see cref="IReranker" /> such as <c>RrfReranker</c> or <c>WeightedReranker</c>.
    /// </remarks>
    public string? Rerank { get; set; }

    /// <summary>
    /// A structured function-score reranker (boost / decay / model) to apply to the search, mapped to the
    /// proto <c>schema.FunctionScore</c>. When set, it takes precedence over <see cref="Rerank" />.
    /// </summary>
    public IReranker? FunctionScoreReranker { get; set; }

    /// <summary>
    /// Named expression-template values used by the <see cref="Expression" /> filter, for parameterized filters.
    /// </summary>
    public IDictionary<string, object> FilterTemplates { get; } = new Dictionary<string, object>();

    /// <summary>
    /// The highlighter settings (key-value pairs) used to highlight matched terms in the results.
    /// </summary>
    public IDictionary<string, string> Highlighter { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The type of highlighter to apply (lexical or semantic). When set, it is sent as the highlighter's
    /// <c>type</c>, matching the Java SDK's <c>Highlighter.highlightType()</c>. When <c>null</c>, the server
    /// applies its default (lexical).
    /// </summary>
    public HighlightType? HighlightType { get; set; }

    /// <summary>
    /// The typed index/search parameters as wire key-value pairs, mirroring pymilvus's <c>search_params</c> bag.
    /// The values keep their CLR types so the "params" JSON can serialize numbers/bools correctly.
    /// </summary>
    internal IReadOnlyList<KeyValuePair<string, object?>> TypedIndexParams
    {
        get
        {
            var list = new List<KeyValuePair<string, object?>>();
            if (Ef is { } ef)
            {
                list.Add(new KeyValuePair<string, object?>("ef", ef));
            }

            if (Nprobe is { } nprobe)
            {
                list.Add(new KeyValuePair<string, object?>("nprobe", nprobe));
            }

            if (DropRatioSearch is { } dropRatio)
            {
                list.Add(new KeyValuePair<string, object?>("drop_ratio_search", dropRatio));
            }

            if (AnalyzerName is not null)
            {
                list.Add(new KeyValuePair<string, object?>("analyzer_name", AnalyzerName));
            }

            if (Hints is not null)
            {
                list.Add(new KeyValuePair<string, object?>("hints", Hints));
            }

            if (JsonPath is not null)
            {
                list.Add(new KeyValuePair<string, object?>("json_path", JsonPath));
            }

            if (JsonType is not null)
            {
                list.Add(new KeyValuePair<string, object?>("json_type", JsonType));
            }

            if (PageRetainOrder is { } retainOrder)
            {
                list.Add(new KeyValuePair<string, object?>("page_retain_order", retainOrder));
            }

            return list;
        }
    }

    /// <summary>
    /// Validates the typed index/search parameters against the server-accepted ranges, mirroring pymilvus's
    /// client-side checks (round_decimal range, drop_ratio_search in [0, 1), json_type cast set).
    /// </summary>
    internal void Validate()
    {
        if (RoundDecimal is { } roundDecimal && (roundDecimal < -1 || roundDecimal > 6))
        {
            throw new ArgumentOutOfRangeException(
                nameof(RoundDecimal), roundDecimal, "RoundDecimal must be between -1 and 6 (the server range; -1 disables rounding).");
        }

        if (DropRatioSearch is { } dropRatio && (dropRatio < 0 || dropRatio >= 1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(DropRatioSearch), dropRatio, "DropRatioSearch must be in [0, 1).");
        }

        if (JsonType is { } jsonType and not ("Int8" or "Int16" or "Int32" or "Int64" or "Bool" or "VarChar"))
        {
            throw new ArgumentException(
                "JsonType must be one of Int8, Int16, Int32, Int64, Bool, VarChar.", nameof(JsonType));
        }

        // A key supplied through both the typed property and ExtraParameters would be sent twice (the flat
        // keys disagreeing with the params JSON), mirroring pymilvus's "expect just one" rejection for
        // dual-supplied search parameters.
        foreach (KeyValuePair<string, object?> parameter in TypedIndexParams)
        {
            if (ExtraParameters.ContainsKey(parameter.Key))
            {
                throw new ArgumentException(
                    $"The search parameter '{parameter.Key}' is set both via its typed property and via " +
                    "ExtraParameters; provide it only once.", parameter.Key);
            }
        }

        // Same "provide it only once" rule for the typed order-by list: the proxy's parseOrderByFields keeps
        // the first order_by_fields match, so a caller-supplied ExtraParameters copy would silently win and
        // the typed list would be ignored.
        if (OrderByFields.Count > 0 && ExtraParameters.ContainsKey(Constants.OrderByFields))
        {
            throw new ArgumentException(
                "order_by_fields is set both via OrderByFields and via ExtraParameters; provide it only once.",
                nameof(OrderByFields));
        }
    }
}

/// <summary>
/// The type of highlighter used to produce matched-term highlights in search results.
/// </summary>
public enum HighlightType
{
    /// <summary>
    /// Lexical (BM25/keyword-based) highlighting. The server default.
    /// </summary>
    Lexical = 0,

    /// <summary>
    /// Semantic (embedding-based) highlighting.
    /// </summary>
    Semantic = 1
}
