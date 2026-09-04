using System.Runtime.CompilerServices;

using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A server-side iterator that pages over search results in batches using the <c>search_iter_v2</c> token
/// protocol. Each page yields one <see cref="SingleResult" /> holding that page's top-K hits with their
/// scores, primary keys and output fields (the search iterator accepts a single query vector, so there is
/// exactly one <see cref="SingleResult" /> per page).
/// </summary>
/// <remarks>
/// Consume with <c>await foreach</c>:
/// <code>
/// await foreach (SingleResult page in client.SearchIteratorAsync(new SearchIteratorReq { ... }))
/// {
///     foreach (float score in page.Scores) { ... }
/// }
/// </code>
/// </remarks>
public sealed class SearchIterator : IAsyncEnumerable<SingleResult>
{
    private readonly MilvusClientV2 _client;
    private readonly SearchIteratorReq _request;

    internal SearchIterator(MilvusClientV2 client, SearchIteratorReq request)
    {
        _client = client;
        _request = request;
    }

    /// <inheritdoc />
    public IAsyncEnumerator<SingleResult> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => _client.SearchIteratorCoreAsync(_request, cancellationToken).GetAsyncEnumerator(cancellationToken);
}
