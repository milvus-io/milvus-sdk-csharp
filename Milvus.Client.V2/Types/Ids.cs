namespace Milvus.Client.V2.Types;

/// <summary>
/// A wrapper around an array of IDs returned from a query or search. Can contain either long or string IDs.
/// </summary>
public readonly struct Ids : IEquatable<Ids>
{
    private Ids(IReadOnlyList<long> longIds)
        => LongIds = longIds;

    private Ids(IReadOnlyList<string> stringIds)
        => StringIds = stringIds;

    /// <summary>
    /// The long IDs, or <c>null</c> when the primary key is a string.
    /// </summary>
    public IReadOnlyList<long>? LongIds { get; }

    /// <summary>
    /// The string IDs, or <c>null</c> when the primary key is an integer.
    /// </summary>
    public IReadOnlyList<string>? StringIds { get; }

    internal static Ids FromGrpc(Grpc.IDs grpcIds)
        => grpcIds.IdFieldCase switch
        {
            Grpc.IDs.IdFieldOneofCase.None => default,
            Grpc.IDs.IdFieldOneofCase.IntId => new Ids(grpcIds.IntId.Data),
            Grpc.IDs.IdFieldOneofCase.StrId => new Ids(grpcIds.StrId.Data),
            _ => throw new NotSupportedException("Invalid ID type in search results: " + grpcIds.IdFieldCase)
        };

    /// <summary>
    /// Builds a <see cref="Ids" /> from already-sliced per-query id arrays, or <c>default</c> when both
    /// are empty.
    /// </summary>
    internal static Ids FromSlices(IReadOnlyList<long>? longIds, IReadOnlyList<string>? stringIds)
        => longIds is { Count: > 0 }
            ? new Ids(longIds)
            : stringIds is { Count: > 0 }
                ? new Ids(stringIds)
                : default;

    /// <inheritdoc />
    public bool Equals(Ids other)
    {
        switch (this)
        {
            case { LongIds: IReadOnlyList<long> longIds }:
                if (other.LongIds is not IReadOnlyList<long> otherLongIds || longIds.Count != otherLongIds.Count)
                {
                    return false;
                }
                for (int i = 0; i < longIds.Count; i++)
                {
                    if (longIds[i] != otherLongIds[i]) return false;
                }
                return true;

            case { StringIds: IReadOnlyList<string> stringIds }:
                if (other.StringIds is not IReadOnlyList<string> otherStringIds || stringIds.Count != otherStringIds.Count)
                {
                    return false;
                }
                for (int i = 0; i < stringIds.Count; i++)
                {
                    if (stringIds[i] != otherStringIds[i]) return false;
                }
                return true;

            default:
                // Neither this nor other carries an ID list (default instance).
                return other.LongIds is null && other.StringIds is null;
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Ids other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        switch (this)
        {
            case { LongIds: IReadOnlyList<long> longIds }:
                foreach (long id in longIds) hash.Add(id);
                break;
            case { StringIds: IReadOnlyList<string> stringIds }:
                foreach (string id in stringIds) hash.Add(id);
                break;
        }
        return hash.ToHashCode();
    }

    /// <summary>Compares the two ID lists for equality.</summary>
    public static bool operator ==(Ids left, Ids right) => left.Equals(right);

    /// <summary>Compares the two ID lists for inequality.</summary>
    public static bool operator !=(Ids left, Ids right) => !(left == right);
}
