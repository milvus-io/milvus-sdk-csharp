namespace Milvus.Client.V2.Responses.Partition;

/// <summary>
/// The result of a <c>ListPartitions</c> operation.
/// </summary>
public sealed class ListPartitionsResp
{
    private ListPartitionsResp(
        IReadOnlyList<string> partitionNames, IReadOnlyList<long> partitionIds,
        IReadOnlyList<ulong> createdTimestamps, IReadOnlyList<ulong> createdUtcTimestamps,
        IReadOnlyList<long> inMemoryPercentages)
    {
        PartitionNames = partitionNames;
        PartitionIds = partitionIds;
        CreatedTimestamps = createdTimestamps;
        CreatedUtcTimestamps = createdUtcTimestamps;
        InMemoryPercentages = inMemoryPercentages;
    }

    internal static ListPartitionsResp FromGrpc(Grpc.ShowPartitionsResponse response)
        => new(
            response.PartitionNames.ToList(),
            response.PartitionIDs.ToList(),
            response.CreatedTimestamps.ToList(),
            response.CreatedUtcTimestamps.ToList(),
#pragma warning disable CS0612 // InMemoryPercentages is [deprecated] in the proto but read for C++ parity.
            response.InMemoryPercentages.ToList());
#pragma warning restore CS0612

    /// <summary>
    /// The names of the partitions in the collection.
    /// </summary>
    public IReadOnlyList<string> PartitionNames { get; }

    /// <summary>
    /// The IDs of the partitions in the collection.
    /// </summary>
    public IReadOnlyList<long> PartitionIds { get; }

    /// <summary>
    /// The hybrid timestamps at which the partitions were created, aligned with <see cref="PartitionNames" />.
    /// </summary>
    public IReadOnlyList<ulong> CreatedTimestamps { get; }

    /// <summary>
    /// The UTC timestamps at which the partitions were created, aligned with <see cref="PartitionNames" />.
    /// </summary>
    public IReadOnlyList<ulong> CreatedUtcTimestamps { get; }

    /// <summary>
    /// The percentage of each partition loaded into memory, aligned with <see cref="PartitionNames" />.
    /// </summary>
    public IReadOnlyList<long> InMemoryPercentages { get; }
}
