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

    internal QueryIterator(MilvusClientV2 client, QueryIteratorReq request)
    {
        _client = client;
        _request = request;
    }

    /// <inheritdoc />
    public IAsyncEnumerator<IReadOnlyList<FieldData>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => _client.QueryIteratorCoreAsync(_request, cancellationToken).GetAsyncEnumerator(cancellationToken);
}
