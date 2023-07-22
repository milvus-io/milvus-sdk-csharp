using IO.Milvus.Grpc;

namespace IO.Milvus;

/// <summary>
/// A wrapper around an array of IDs returned from a query or search. Can contain either long or string IDs.
/// </summary>
public readonly struct MilvusIds
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
}
