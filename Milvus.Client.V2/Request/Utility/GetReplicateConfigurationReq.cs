namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the replication configuration of a cluster.
/// </summary>
public sealed class GetReplicateConfigurationReq
{
    internal static Grpc.GetReplicateConfigurationRequest ToGrpcRequest()
        => new();
}
