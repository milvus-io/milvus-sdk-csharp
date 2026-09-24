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
    }
}
