using System.Runtime.CompilerServices;

using Milvus.Client.V2.Requests.Dql;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A server-side iterator that pages over search results in batches using the <c>search_iter_v2</c> token
/// protocol.
/// </summary>
/// <remarks>
/// Consume with <c>await foreach</c>:
/// <code>
/// await foreach (IReadOnlyList&lt;FieldData&gt; batch in client.SearchIteratorAsync(new SearchIteratorReq { ... }))
/// {
///     // each batch is a page of rows
/// }
/// </code>
/// </remarks>
public sealed class SearchIterator : IAsyncEnumerable<IReadOnlyList<FieldData>>
{
    private readonly MilvusClientV2 _client;
    private readonly SearchIteratorReq _request;

    internal SearchIterator(MilvusClientV2 client, SearchIteratorReq request)
    {
        _client = client;
        _request = request;
    }

    /// <inheritdoc />
    public IAsyncEnumerator<IReadOnlyList<FieldData>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => _client.SearchIteratorCoreAsync(_request, cancellationToken).GetAsyncEnumerator(cancellationToken);
}
