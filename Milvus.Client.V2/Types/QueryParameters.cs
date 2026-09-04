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
}
