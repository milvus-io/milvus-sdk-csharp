namespace Milvus.Client.V2.Types;

/// <summary>
/// A snapshot of a <see cref="QueryIterator" /> position, used to resume pagination from where a previous
/// iterator left off.
/// </summary>
/// <remarks>
/// The cursor captures the session timestamp of the original iterator, the last primary key returned and,
/// for <c>element_filter</c> iterators, the last matched element offset. Resume by passing it back through
/// <see cref="Requests.Dql.QueryIteratorReq.Cursor" />; the resumed iterator skips the offset seek and
/// starts from the captured key on the captured MVCC snapshot.
/// </remarks>
public sealed class QueryIteratorCursor
{
    /// <summary>
    /// Creates a new cursor.
    /// </summary>
    /// <param name="sessionTs">The session timestamp of the iterator that produced this cursor.</param>
    /// <param name="intPk">The last returned integer primary key, when the collection's primary key is an
    /// integer type.</param>
    /// <param name="strPk">The last returned string primary key, when the collection's primary key is a
    /// <see cref="DataType.VarChar" />.</param>
    /// <param name="lastElementOffset">For an <c>element_filter</c> iterator, the last matched element
    /// offset; <c>null</c> otherwise.</param>
    /// <remarks>
    /// <paramref name="intPk" /> and <paramref name="strPk" /> are mutually exclusive, but **both may be left
    /// unset**: a keyless cursor carries only the pinned <paramref name="sessionTs" /> and resumes iteration
    /// from the beginning of the collection on that same MVCC snapshot. A
    /// <paramref name="sessionTs" /> of zero means no snapshot is pinned: the resumed iterator lets the server
    /// pick its own snapshot per page.
    /// </remarks>
    /// <exception cref="ArgumentException">When <paramref name="intPk" /> and <paramref name="strPk" /> are
    /// both set, when <paramref name="lastElementOffset" /> is set without a primary key, or when
    /// <paramref name="lastElementOffset" /> is negative.</exception>
    public QueryIteratorCursor(ulong sessionTs, long? intPk = null, string? strPk = null, long? lastElementOffset = null)
    {
        if (intPk is not null && strPk is not null)
        {
            throw new ArgumentException(
                "intPk and strPk are mutually exclusive; a primary key is either an integer or a string.",
                nameof(strPk));
        }

        if (lastElementOffset is not null && intPk is null && strPk is null)
        {
            throw new ArgumentException(
                "An element offset requires a primary key; a keyless cursor cannot carry a lastElementOffset.",
                nameof(lastElementOffset));
        }

        if (lastElementOffset < 0)
        {
            throw new ArgumentException("lastElementOffset must not be negative.", nameof(lastElementOffset));
        }

        SessionTs = sessionTs;
        IntPk = intPk;
        StrPk = strPk;
        LastElementOffset = lastElementOffset;
    }

    /// <summary>
    /// The session timestamp of the iterator that produced this cursor, pinning the MVCC snapshot the
    /// resumed iterator continues on.
    /// </summary>
    public ulong SessionTs { get; }

    /// <summary>
    /// The last returned integer primary key, or <c>null</c> when the primary key is a string.
    /// </summary>
    public long? IntPk { get; }

    /// <summary>
    /// The last returned string primary key, or <c>null</c> when the primary key is an integer.
    /// </summary>
    public string? StrPk { get; }

    /// <summary>
    /// For an <c>element_filter</c> iterator, the last matched element offset; <c>null</c> for ordinary
    /// iterators.
    /// </summary>
    public long? LastElementOffset { get; }
}
