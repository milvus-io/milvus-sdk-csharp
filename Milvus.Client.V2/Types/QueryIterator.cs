using System.Runtime.CompilerServices;

using Milvus.Client.V2.Requests.Dql;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A server-side iterator that pages over query results in batches, driven by the primary key cursor.
/// </summary>
/// <remarks>
/// Consume with <c>await foreach</c>:
/// <code>
/// await foreach (IReadOnlyList&lt;FieldData&gt; batch in client.QueryIteratorAsync(new QueryIteratorReq { ... }))
/// {
///     // each batch is a page of rows
/// }
/// </code>
/// </remarks>
public sealed class QueryIterator : IAsyncEnumerable<IReadOnlyList<FieldData>>
{
    private readonly MilvusClientV2 _client;
    private readonly QueryIteratorReq _request;
    private readonly string? _clusterId;
    private QueryIteratorPosition _position = new();

    internal QueryIterator(MilvusClientV2 client, QueryIteratorReq request, string? clusterId = null)
    {
        _client = client;
        _request = request;
        _clusterId = clusterId;
    }

    /// <inheritdoc />
    public IAsyncEnumerator<IReadOnlyList<FieldData>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        // Each enumeration of the same iterator starts from a fresh position: a second `await foreach` over
        // this iterator restarts from the request's original expression and offset (or the supplied cursor)
        // instead of silently resuming from the previous run's last page. GetCursor() reflects the most
        // recent enumeration's position.
        _position = new QueryIteratorPosition();
        return _client.QueryIteratorCoreAsync(_request, _clusterId, _position, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
    }

    /// <summary>
    /// Captures the current iterator position as a resumable cursor, which can be passed back through
    /// <see cref="QueryIteratorReq.Cursor" /> to continue pagination from this point on the same MVCC
    /// snapshot. Returns <c>null</c> when no snapshot has been established yet (no page consumed, or a fresh
    /// iterator whose first page is still in flight).
    /// </summary>
    /// <remarks>
    /// The cursor is updated as each page is returned. Once a snapshot is
    /// pinned it is always capturable -- including before any primary key is returned, in which case it is a
    /// keyless cursor that resumes from the beginning of the collection on that snapshot. For
    /// <c>element_filter</c> iterators the cursor also carries the last matched element offset.
    /// </remarks>
    public QueryIteratorCursor? GetCursor()
    {
        QueryIteratorPosition position = _position;
        return position.SessionTs == 0 ? null : position.ToCursor();
    }
}

// Mutable runtime position of a QueryIterator, written back by the facade core as pages are returned so
// GetCursor() can snapshot it. Not thread-safe; a cursor should be captured while the iterator is consumed.
internal sealed class QueryIteratorPosition
{
    public ulong SessionTs;
    public long? IntPk;
    public string? StrPk;
    public long? LastElementOffset;

    public QueryIteratorCursor ToCursor() => new(SessionTs, IntPk, StrPk, LastElementOffset);
}
