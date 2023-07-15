using System;

namespace IO.Milvus;

/// <summary>
/// Milvus partition
/// </summary>
/// <remarks>
/// Milvus allows you to divide the bulk of vector data into a small number of partitions. 
/// Search and other operations can then be limited to one partition to improve the performance.
/// </remarks>
public sealed class MilvusPartition
{
    /// <summary>
    /// Construct a milvus partition.
    /// </summary>
    /// <param name="partitionId">Partition id.</param>
    /// <param name="partitionName">Partition name.</param>
    /// <param name="createdUtcTimestamp">Created datetime.</param>
    /// <param name="inMemoryPercentage">Load percentage on query node.</param>
    public MilvusPartition(
        long partitionId,
        string partitionName,
        DateTime createdUtcTimestamp,
        long inMemoryPercentage)
    {
        PartitionId = partitionId;
        PartitionName = partitionName;
        CreatedUtcTime = createdUtcTimestamp;
        InMemoryPercentage = inMemoryPercentage;
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
    /// Load percentage on query node.
    /// </summary>
    public long InMemoryPercentage { get; }

    /// <summary>
    /// Create utc time.
    /// </summary>
    /// <remarks>
    /// If you want to get a local time, you can use <see cref="DateTime.ToLocalTime"/>.
    /// </remarks>
    public DateTime CreatedUtcTime { get; }

    /// <summary>
    /// Return string value of <see cref="MilvusPartition"/>.
    /// </summary>
    public override string ToString()
        => $"MilvusPartition: {{{nameof(PartitionName)}: {PartitionName}, {nameof(PartitionId)}: {PartitionId}, {nameof(CreatedUtcTime)}:{CreatedUtcTime}, {nameof(InMemoryPercentage)}: {InMemoryPercentage}}}";
}
