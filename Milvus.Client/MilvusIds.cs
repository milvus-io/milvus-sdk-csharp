using System.Diagnostics;

namespace Milvus.Client;

/// <summary>
/// A wrapper around an array of IDs returned from a query or search. Can contain either long or string IDs.
/// </summary>
public readonly struct MilvusIds : IEquatable<MilvusIds>
{
    private MilvusIds(IReadOnlyList<long> longIds)
        => LongIds = longIds;

    private MilvusIds(IReadOnlyList<string> stringIds)
        => StringIds = stringIds;

    /// <summary>
    /// Int id.
    /// </summary>
    public IReadOnlyList<long>? LongIds { get; }

    /// <summary>
    /// String id.
    /// </summary>
    public IReadOnlyList<string>? StringIds { get; }

    internal static MilvusIds FromGrpc(Grpc.IDs grpcIds)
        => grpcIds.IdFieldCase switch
        {
            IDs.IdFieldOneofCase.None => default,
            IDs.IdFieldOneofCase.IntId => new MilvusIds(grpcIds.IntId.Data),
            IDs.IdFieldOneofCase.StrId => new MilvusIds(grpcIds.StrId.Data),
            _ => throw new NotSupportedException("Invalid ID type in search results: " + grpcIds.IdFieldCase)
        };

    /// <inheritdoc />
    public bool Equals(MilvusIds other)
    {
        switch (this)
        {
            case { LongIds: IReadOnlyList<long> longIds }:
                if (other.LongIds is not IReadOnlyList<long> otherLongIds ||
                    longIds.Count != otherLongIds.Count)
                {
                    return false;
                }

                for (int i = 0; i < longIds.Count; i++)
                {
                    if (longIds[i] != otherLongIds[i])
                    {
                        return false;
                    }
                }

                return true;

            case { StringIds: IReadOnlyList<string> stringIds }:
                if (other.StringIds is not IReadOnlyList<string> otherStringIds ||
                    stringIds.Count != otherStringIds.Count)
                {
                    return false;
                }

                for (int i = 0; i < stringIds.Count; i++)
                {
                    if (stringIds[i] != otherStringIds[i])
                    {
                        return false;
                    }
                }

                return true;

            default:
                Debug.Assert(this == default);
                return other == default;
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is MilvusIds other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hashCode = new();

        switch (this)
        {
            case { LongIds: IReadOnlyList<long> longIds }:
                foreach (long id in longIds)
                {
                    hashCode.Add(id);
                }

                break;

            case { StringIds: IReadOnlyList<string> stringIds }:
                foreach (string id in stringIds)
                {
                    hashCode.Add(id);
                }

                break;
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Compares the two ID lists for equality.
    /// </summary>
    public static bool operator ==(MilvusIds left, MilvusIds right)
        => left.Equals(right);

    /// <summary>
    /// Compares the two ID lists for equality.
    /// </summary>
    public static bool operator !=(MilvusIds left, MilvusIds right)
        => !(left == right);

}
