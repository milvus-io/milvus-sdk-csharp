namespace Milvus.Client;

/// <summary>
/// A set of optional parameters for performing a query via <see cref="MilvusCollection.QueryAsync" />.
/// </summary>
public class QueryParameters
{
    internal List<string>? OutputFieldsInternal { get; private set; }
    internal List<string>? PartitionNamesInternal { get; private set; }

    /// <summary>
    /// The maximum number of records to return, also known as 'topk'. If set, the sum of this parameter and of
    /// <see cref="Offset" /> must be between 1 and 16384.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Number of entities to skip during the search. If set, the sum of this parameter and of <see cref="Limit" /> must
    /// be between 1 and 16384.
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
}
