using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to iterate over query results in batches using a server-side iterator.
/// </summary>
public sealed class QueryIteratorReq
{
    /// <summary>
    /// The name of the collection to query.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The boolean expression identifying the rows to return. When omitted, the iterator returns all rows
    /// ordered by primary key.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// The optional query parameters.
    /// </summary>
    public QueryParameters? Parameters { get; set; }

    /// <summary>
    /// The number of rows to fetch per batch. Defaults to 1000.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// When true (default), the server stops reducing results once the top-K best results are found, which can
    /// improve performance at the cost of exactness. When false, the server performs a full reduction.
    /// </summary>
    public bool ReduceStopForBest { get; set; } = true;

    /// <summary>
    /// The row-level security (RLS) principal for the operation. When set, Milvus applies the row policies of
    /// the collection to this query iterator.
    /// </summary>
    public string? RlsPrincipal { get; set; }

    /// <summary>
    /// Whether the operation should skip row-level security (RLS) enforcement. When <c>null</c>, the server
    /// default applies.
    /// </summary>
    public bool? SkipRls { get; set; }

    /// <summary>
    /// An optional cursor from a previous <see cref="MilvusClientV2.QueryIteratorAsync" /> (captured via
    /// <see cref="QueryIterator.GetCursor" />). When set, the iterator resumes from that position on the
    /// captured MVCC snapshot, skipping the offset seek.
    /// </summary>
    public QueryIteratorCursor? Cursor { get; set; }
    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        if (BatchSize < 1 || BatchSize > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), BatchSize, "Batch size must be between 1 and 16384");
        }

        if (Parameters?.Offset is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Parameters), Parameters.Offset,
                "Offset must be non-negative.");
        }

        if (Parameters?.Limit is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Parameters), Parameters.Limit,
                "Limit must be at least 1.");
        }

        // Use long arithmetic so large Offset/Limit values cannot overflow int and bypass the guard,
        // matching QueryReq/BuildSearchRequest parity.
        if (Parameters is { Offset: { } offset, Limit: { } limit } && (long)offset + limit > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(Parameters), Parameters.Offset,
                $"The sum of Offset and Limit must not exceed 16384 (got {(long)offset + limit}).");
        }

        // The iterator pages by a primary-key cursor (`pk > lastPk`), which assumes primary-key-ordered,
        // non-grouped output; arbitrary ordering/grouping would silently produce inconsistent pages. The
        // Reject them up front instead of
        // emitting the params on every page -- both the typed properties and the raw ExtraParameters escape
        // hatch (which is forwarded verbatim onto every page).
        if (Parameters is { OrderByFields.Count: > 0 } or { GroupByFields.Count: > 0 }
            || Parameters?.ExtraParameters.ContainsKey(Constants.OrderByFields) == true
            || Parameters?.ExtraParameters.ContainsKey(Constants.GroupByFields) == true)
        {
            throw new ArgumentException(
                "OrderByFields and GroupByFields (typed or via ExtraParameters) are not supported with a " +
                "query iterator: pk-cursor paging requires primary-key-ordered, non-grouped output " +
                "(matching the Java/PyMilvus query iterators).",
                nameof(Parameters));
        }
    }
}
