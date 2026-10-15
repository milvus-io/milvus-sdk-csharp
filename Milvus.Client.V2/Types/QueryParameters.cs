using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A set of optional parameters for performing a query.
/// </summary>
public sealed class QueryParameters
{
    internal List<string>? OutputFieldsInternal { get; private set; }
    internal List<string>? PartitionNamesInternal { get; private set; }

    /// <summary>
    /// The maximum number of rows to return. If set, the sum of this parameter and <see cref="Offset" /> must be
    /// between 1 and 16384.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of rows to skip. If set, the sum of this parameter and <see cref="Limit" /> must be between 1 and
    /// 16384.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// The consistency level to be used in the query. Defaults to the consistency level configured for the
    /// collection.
    /// </summary>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// If set, guarantee that the query is performed after any updates up to the provided timestamp.
    /// </summary>
    public ulong? GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Specifies an optional time travel timestamp; the query will get results based on the data at that point in
    /// time.
    /// </summary>
    public ulong? TimeTravelTimestamp { get; set; }

    /// <summary>
    /// An optional list of partitions to be queried in the collection.
    /// </summary>
    public IList<string> PartitionNames => PartitionNamesInternal ??= new();

    /// <summary>
    /// The names of fields to be returned from the query.
    /// </summary>
    public IList<string> OutputFields => OutputFieldsInternal ??= new();

    /// <summary>
    /// Whether to ignore the growing segments during the query.
    /// </summary>
    public bool? IgnoreGrowing { get; set; }

    /// <summary>
    /// The timezone used for the query, e.g. <c>"+08:00"</c> or <c>"America/New_York"</c>.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Named expression-template values used by the query's filter expression, for parameterized filters.
    /// </summary>
    /// <example>
    /// <code>
    /// Expression = "age &gt; {minAge} and city in {cities}"
    /// Parameters.FilterTemplates["minAge"] = 21;
    /// Parameters.FilterTemplates["cities"] = new[] { "NYC", "SFO" };
    /// </code>
    /// Placeholders are written with curly braces inside the expression; values may be a scalar
    /// (bool/int/long/float/double/string) or an enumerable of those.
    /// </example>
    public IDictionary<string, object> FilterTemplates { get; } = new Dictionary<string, object>();

    /// <summary>
    /// The primary key values identifying the rows to return. Mutually exclusive with the query's expression;
    /// when set, an expression is generated from the collection's primary key field.
    /// </summary>
    public IReadOnlyList<object>? Ids { get; set; }

    /// <summary>
    /// Additional query parameters passed through to the server verbatim (e.g. <c>"advanced_query"</c>),
    /// mirroring the Java SDK's <c>QueryReq.queryParams</c> map.
    /// </summary>
    public IDictionary<string, string> ExtraParameters { get; } = new Dictionary<string, string>();
}
