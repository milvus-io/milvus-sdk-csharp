namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// Detailed information about a snapshot, as returned by <c>DescribeSnapshot</c>.
/// </summary>
public sealed class DescribeSnapshotResp
{
    private DescribeSnapshotResp(
        string name, string description, string collectionName, IReadOnlyList<string> partitionNames,
        long createTs, string s3Location)
    {
        Name = name;
        Description = description;
        CollectionName = collectionName;
        PartitionNames = partitionNames;
        CreateTs = createTs;
        S3Location = s3Location;
    }

    internal static DescribeSnapshotResp FromGrpc(Grpc.DescribeSnapshotResponse response)
        => new(
            response.Name,
            response.Description,
            response.CollectionName,
            response.PartitionNames.ToList(),
            response.CreateTs,
            response.S3Location);

    /// <summary>
    /// The name of the snapshot.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of the snapshot.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The name of the collection the snapshot belongs to.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The names of the partitions captured in the snapshot.
    /// </summary>
    public IReadOnlyList<string> PartitionNames { get; }

    /// <summary>
    /// The creation timestamp; all stream data before this timestamp is flushed to the segment list.
    /// </summary>
    public long CreateTs { get; }

    /// <summary>
    /// The S3 location of the snapshot meta file, when the snapshot is exported to S3.
    /// </summary>
    public string S3Location { get; }
}
