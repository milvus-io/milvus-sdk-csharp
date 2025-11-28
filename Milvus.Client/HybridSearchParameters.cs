namespace Milvus.Client;

/// <summary>
/// A set of optional parameters for performing a hybrid search via
/// <see cref="MilvusCollection.HybridSearchAsync" />.
/// </summary>
public class HybridSearchParameters
{
    internal List<string>? OutputFieldsInternal { get; private set; }
    internal List<string>? PartitionNamesInternal { get; private set; }

    /// <summary>
    /// An optional list of partitions to be searched in the collection.
    /// </summary>
    public IList<string> PartitionNames => PartitionNamesInternal ??= [];

    /// <summary>
    /// The names of fields to be returned from the search. Vector fields currently cannot be returned.
    /// </summary>
    public IList<string> OutputFields => OutputFieldsInternal ??= [];

    /// <summary>
    /// The consistency level to be used in the search. Defaults to the collection's consistency level.
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
    /// Specifies an optional time travel timestamp; the search will get results based on the data at that point in time.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/v2.1.x/timetravel.md"/>.
    /// </remarks>
    public ulong? TimeTravelTimestamp { get; set; }

    /// <summary>
    /// Specifies the decimal place of the returned results.
    /// </summary>
    public long? RoundDecimal { get; set; }

    /// <summary>
    /// Group search results by the specified field.
    /// </summary>
    /// <remarks>
    /// See <see href="https://milvus.io/docs/single-vector-search.md#Grouping-search" /> for more information.
    /// </remarks>
    public string? GroupByField { get; set; }

    /// <summary>
    /// The maximum number of groups to return when using group-by.
    /// </summary>
    public int? GroupSize { get; set; }

    /// <summary>
    /// Whether to return only strict groups when using group-by.
    /// When true, only groups with exactly <see cref="GroupSize"/> results are returned.
    /// </summary>
    public bool? StrictGroupSize { get; set; }
}
