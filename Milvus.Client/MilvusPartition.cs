namespace Milvus.Client;

/// <summary>
/// Milvus partition
/// </summary>
/// <remarks>
/// Milvus allows you to divide the bulk of vector data into a small number of partitions.
/// Search and other operations can then be limited to one partition to improve the performance.
/// </remarks>
public sealed class MilvusPartition
{
    internal MilvusPartition(
        long partitionId,
        string partitionName,
        ulong creationTimestamp)
    {
        PartitionId = partitionId;
        PartitionName = partitionName;
        CreationTimestamp = creationTimestamp;
    }

    /// <summary>
    /// Partition id.
    /// </summary>
    public long PartitionId { get; }

    /// <summary>
    /// Partition name.
    /// </summary>
    public string PartitionName { get; }

    /// <summary>
    /// An opaque identifier for the point in time in which the partition was created. Can be passed to
    /// <see cref="MilvusCollection.SearchAsync{T}(string, IReadOnlyList{ReadOnlyMemory{T}}, SimilarityMetricType, int, SearchParameters, CancellationToken)" />
    /// or <see cref="MilvusCollection.QueryAsync" /> as a <i>guarantee timestamp</i> or as a <i>time travel timestamp</i>.
    /// </summary>
    /// <remarks>
    /// For more details, see <see href="https://milvus.io/docs/timestamp.md" />.
    /// </remarks>
    public ulong CreationTimestamp { get; }

    /// <summary>
    /// Return string value of <see cref="MilvusPartition"/>.
    /// </summary>
    public override string ToString()
        => $"MilvusPartition: {{{nameof(PartitionName)}: {PartitionName}, {nameof(PartitionId)}: {PartitionId}, {nameof(CreationTimestamp)}:{CreationTimestamp}}}";
}
