namespace Milvus.Client.V2.Responses.Partition;

/// <summary>
/// The result of a <c>HasPartition</c> check.
/// </summary>
public sealed class HasPartitionResp
{
    private HasPartitionResp(bool has) => Has = has;
    internal static HasPartitionResp FromGrpc(Grpc.BoolResponse response) => new(response.Value);

    /// <summary>
    /// Whether the partition exists in the collection.
    /// </summary>
    public bool Has { get; }
}
