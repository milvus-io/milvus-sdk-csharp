using IO.Milvus.Client;

namespace IO.Milvus;

/// <summary>
/// Represents a set of parameters required to perform a vector similarity search via
/// <see cref="MilvusClient.SearchAsync{T}" />.
/// </summary>
public class SearchParameters
{
    /// <summary>
    /// Number of entities to skip during the search. The sum of this parameter and <c>Limit</c> should be less than
    /// 16384.
    /// </summary>
    // TODO: IMPLEMENT
    public int Offset { get; set; }

    /// <summary>
    /// An optional list of partitions to be searched in the collection.
    /// </summary>
    public IList<string> PartitionNames { get; } = new List<string>();

    /// <summary>
    /// The consistency level to be used in the query.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/consistency.md" />.
    /// </remarks>
    public MilvusConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// The names of fields to be returned from the search. Vector fields currently cannot be returned.
    /// </summary>
    public IList<string> OutputFields { get; } = new List<string>();

    /// <summary>
    /// Specifies an optional travel timestamp; the query will get results based on the data at that point in time.
    /// </summary>
    /// <remarks>
    /// The default value is 0, with which the server executes the query on a full data view. For more information please refer to Search with Time Travel.
    /// <see href="https://milvus.io/docs/v2.1.x/timetravel.md"/>
    /// </remarks>
    // TODO: UTC DateTime? Or is this an internal Milvus timestamp, https://github.com/milvus-io/milvus/blob/master/docs/design_docs/20211214-milvus_hybrid_ts.md?
    public long? TravelTimestamp { get; set; }

    /// <summary>
    /// If set, guarantee that the search operation will be performed after any updates up to the provided timestamp.
    /// If a query node isn't yet up to date for the timestamp, it waits until the missing data is received.
    /// If unset, the server executes the search immediately.
    /// </summary>
    public long? GuaranteeTimestamp { get; set; }

    /// <summary>
    /// An optional boolean expression to filter scalar fields before performing the vector similarity search.
    /// </summary>
    public string? Expr { get; set; }

    /// <summary>
    /// Specifies the decimal place of the returned results.
    /// </summary>
    public long? RoundDecimal { get; set; }

    /// <summary>
    /// Search parameter(s) specific to the specified index type.
    /// </summary>
    /// <remarks>
    /// See <see href="https://milvus.io/docs/index.md" /> for more information.
    /// </remarks>
    public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to ignore growing segments during similarity searches. Defaults to <c>false</c>, indicating that
    /// searches involve growing segments.
    /// </summary>
    public bool? IgnoreGrowing { get; private set; }
}
