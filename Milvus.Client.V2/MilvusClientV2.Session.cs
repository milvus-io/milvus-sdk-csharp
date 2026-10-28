using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a session bound to the given cluster ID, scoped to a specific cluster of a global-cluster
    /// (Zilliz Cloud) deployment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A session is a lightweight view of this client that exposes only the DQL query interfaces
    /// (<see cref="MilvusClientV2Session.SearchAsync" />, <see cref="MilvusClientV2Session.HybridSearchAsync" />,
    /// <see cref="MilvusClientV2Session.QueryAsync" />, <see cref="MilvusClientV2Session.GetAsync" />,
    /// <see cref="MilvusClientV2Session.QueryIteratorAsync" /> and
    /// <see cref="MilvusClientV2Session.SearchIteratorAsync" />). Every DQL request issued through the session
    /// carries the bound <c>cluster_id</c> as a search/query/rank parameter, so the request is routed to the
    /// target cluster instead of the client's default routing. The session shares the parent client's
    /// connection and caches; <see cref="MilvusClientV2Session.Close" /> only makes the session unusable and
    /// does not disconnect the parent client.
    /// </para>
    /// <para>
    /// Zilliz Cloud only. Mirrors the <c>session(clusterId)</c> of the Java SDK, <c>Session(clusterId)</c> of the
    /// C++ SDK and <c>session(cluster_id)</c> of PyMilvus.
    /// </para>
    /// </remarks>
    /// <param name="clusterId">The cluster ID to bind the session to.</param>
    /// <returns>A new session bound to the given cluster.</returns>
    /// <exception cref="ArgumentException">When <paramref name="clusterId" /> is null or whitespace.</exception>
    public MilvusClientV2Session Session(string clusterId)
    {
        Verify.NotNullOrWhiteSpace(clusterId);
        return new MilvusClientV2Session(this, clusterId);
    }
}

/// <summary>
/// A cluster-scoped view of <see cref="MilvusClientV2" /> exposing DQL interfaces only.
/// </summary>
/// <remarks>
/// <para>
/// All DQL operations issued through a session are routed to the bound cluster (the wire request carries the
/// <c>cluster_id</c> search/query/rank parameter, which the proxy uses to route in a global-cluster
/// deployment). The session becomes unusable once <see cref="Close" /> is called; closing it does not
/// disconnect the parent client.
/// </para>
/// <para>
/// Zilliz Cloud only. Mirrors the <c>MilvusClientV2Session</c> of the Java SDK, <c>MilvusClientV2Session</c>
/// of the C++ SDK and <c>MilvusClientSession</c> of PyMilvus.
/// </para>
/// </remarks>
public sealed class MilvusClientV2Session
{
    private readonly MilvusClientV2 _parent;

    // Volatile so a Close() from one thread becomes promptly visible to EnsureOpen() on a concurrent session
    // user (the parent client documents that RPC methods may be called concurrently).
    private volatile bool _closed;

    internal MilvusClientV2Session(MilvusClientV2 parent, string clusterId)
    {
        _parent = parent;
        ClusterId = clusterId;
    }

    /// <summary>
    /// The identifier of the cluster this session is bound to.
    /// </summary>
    public string ClusterId { get; }

    /// <summary>
    /// Performs a vector similarity search on the bound cluster. See <see cref="MilvusClientV2.SearchAsync" />.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<SearchResp> SearchAsync(
        SearchReq request,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        return await _parent.SearchCoreAsync(request, ClusterId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a hybrid search on the bound cluster. See <see cref="MilvusClientV2.HybridSearchAsync" />.
    /// </summary>
    /// <param name="request">The hybrid search request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<SearchResp> HybridSearchAsync(
        HybridSearchReq request,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        return await _parent.HybridSearchCoreAsync(request, ClusterId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Queries rows by expression on the bound cluster. See <see cref="MilvusClientV2.QueryAsync" />.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<QueryResp> QueryAsync(
        QueryReq request,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        return await _parent.QueryCoreAsync(request, ClusterId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches rows by primary key on the bound cluster. See <see cref="MilvusClientV2.GetAsync" />.
    /// </summary>
    /// <param name="request">The get request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GetResp> GetAsync(
        GetReq request,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        return await _parent.GetCoreAsync(request, ClusterId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a query iterator on the bound cluster. See <see cref="MilvusClientV2.QueryIteratorAsync" />.
    /// </summary>
    /// <param name="request">The query iterator request.</param>
    public QueryIterator QueryIteratorAsync(QueryIteratorReq request)
    {
        EnsureOpen();
        return _parent.CreateQueryIterator(request, ClusterId);
    }

    /// <summary>
    /// Creates a search iterator on the bound cluster. See <see cref="MilvusClientV2.SearchIteratorAsync" />.
    /// </summary>
    /// <param name="request">The search iterator request.</param>
    public SearchIterator SearchIteratorAsync(SearchIteratorReq request)
    {
        EnsureOpen();
        return _parent.CreateSearchIterator(request, ClusterId);
    }

    /// <summary>
    /// Closes this session. Any subsequent operation on the session fails with
    /// <see cref="MilvusException" />; the parent client is left untouched.
    /// </summary>
    /// <remarks>
    /// Query/search iterators created before <c>Close</c> are unaffected: they hold the parent client and the
    /// bound cluster id directly, so they keep routing to the cluster while they are being consumed (matching
    /// the Java/C++ SDKs).
    /// </remarks>
    public void Close() => _closed = true;

    private void EnsureOpen()
    {
        if (_closed)
        {
            throw new MilvusException(MilvusErrorCode.UnexpectedError, "MilvusClient session is closed");
        }
    }
}
