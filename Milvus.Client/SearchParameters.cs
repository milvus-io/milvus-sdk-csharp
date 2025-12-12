namespace Milvus.Client;

/// <summary>
/// A set of optional parameters for performing a vector similarity search via
/// <see cref="MilvusCollection.SearchAsync{T}(string, IReadOnlyList{ReadOnlyMemory{T}}, SimilarityMetricType, int, SearchParameters, CancellationToken)" />.
/// </summary>
public class SearchParameters
{
    internal List<string>? OutputFieldsInternal { get; private set; }
    internal List<string>? PartitionNamesInternal { get; private set; }

    /// <summary>
    /// Number of entities to skip during the search. The sum of this parameter and <c>Limit</c> should be less than
    /// 16384.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// An optional list of partitions to be searched in the collection.
    /// </summary>
    public IList<string> PartitionNames => PartitionNamesInternal ??= new();

    /// <summary>
    /// The names of fields to be returned from the search. Vector fields currently cannot be returned.
    /// </summary>
    public IList<string> OutputFields => OutputFieldsInternal ??= new();

    /// <summary>
    /// The consistency level to be used in the search. Defaults to the consistency level configured
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/consistency.md" />.
    /// </remarks>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// If set, guarantee that the search operation will be performed after any updates up to the provided timestamp.
    /// If a query node isn't yet up to date for the timestamp, it waits until the missing data is received.
    /// If unset, the server executes the search immediately.
    /// </summary>
    public ulong? GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Specifies an optional time travel timestamp; the search will get results based on the data at that point in
    /// time.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/v2.1.x/timetravel.md"/>.
    /// </remarks>
    public ulong? TimeTravelTimestamp { get; set; }

    /// <summary>
    /// An optional boolean expression to filter scalar fields before performing the vector similarity search.
    /// </summary>
    public string? Expression { get; set; }

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
    public IDictionary<string, string> ExtraParameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to ignore growing segments during similarity searches. Defaults to <c>false</c>, indicating that
    /// searches involve growing segments.
    /// </summary>
    public bool? IgnoreGrowing { get; private set; }

    /// <summary>
    /// Group search results by the specified field.
    /// </summary>
    /// <remarks>
    /// See <see href="https://milvus.io/docs/single-vector-search.md#Grouping-search" /> for more information.
    /// </remarks>
    public string? GroupByField { get; set; }
}
