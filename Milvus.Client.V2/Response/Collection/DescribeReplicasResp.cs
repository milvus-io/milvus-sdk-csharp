using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a describe-replicas operation.
/// </summary>
public sealed class DescribeReplicasResp
{
    private DescribeReplicasResp(IReadOnlyList<ReplicaInfo> replicas)
    {
        Replicas = replicas;
    }

    internal static DescribeReplicasResp FromGrpc(Grpc.GetReplicasResponse response)
        => new(response.Replicas.Select(ReplicaInfo.FromGrpc).ToList());

    /// <summary>
    /// The replicas of the collection.
    /// </summary>
    public IReadOnlyList<ReplicaInfo> Replicas { get; }
}
