namespace Milvus.Client;

/// <summary>
/// Contains results information about an operation which modified rows in a collection.
/// </summary>
public sealed class MutationResult
{
    internal MutationResult(Grpc.MutationResult mutationResult)
    {
        InsertCount = mutationResult.InsertCnt;
        DeleteCount = mutationResult.DeleteCnt;
        UpsertCount = mutationResult.UpsertCnt;
        Acknowledged = mutationResult.Acknowledged;
        SuccessIndex = mutationResult.SuccIndex.ToList();
        ErrorIndex = mutationResult.ErrIndex.ToList();
        Timestamp = mutationResult.Timestamp;
        Ids = MilvusIds.FromGrpc(mutationResult.IDs);
    }

#pragma warning disable CS1591 // Missing documentation
    public bool Acknowledged { get; }
#pragma warning restore CS1591

    /// <summary>
    /// An opaque identifier for the point in time in which the mutation operation occurred. Can be passed to
    /// <see cref="MilvusCollection.SearchAsync{T}(string, IReadOnlyList{ReadOnlyMemory{T}}, SimilarityMetricType, int, SearchParameters, CancellationToken)" />
    /// or <see cref="MilvusCollection.QueryAsync" /> as a <i>guarantee timestamp</i> or as a <i>time travel timestamp</i>.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/timestamp.md" />.
    /// </remarks>
    public ulong Timestamp { get; }

    /// <summary>
    /// The number of inserted rows.
    /// </summary>
    public long InsertCount { get; }

    /// <summary>
    /// The number of deleted rows.
    /// </summary>
    public long DeleteCount { get; }

    /// <summary>
    /// The number of upserted rows.
    /// </summary>
    public long UpsertCount { get; }

    /// <summary>
    /// Success index.
    /// </summary>
    public IReadOnlyList<uint> SuccessIndex { get; }

    /// <summary>
    /// Error index.
    /// </summary>
    public IReadOnlyList<uint> ErrorIndex { get; }

    /// <summary>
    /// The IDs of the rows returned from the search.
    /// </summary>
    public MilvusIds Ids { get; set; }
}
