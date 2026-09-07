using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to update the replication configuration of a cluster.
/// </summary>
public sealed class UpdateReplicateConfigurationReq
{
    /// <summary>
    /// The replication configuration to apply.
    /// </summary>
    public ReplicateConfiguration ReplicateConfiguration { get; set; } = null!;

    internal Grpc.UpdateReplicateConfigurationRequest ToGrpcRequest()
    {
        Verify.NotNull(ReplicateConfiguration);

        return new Grpc.UpdateReplicateConfigurationRequest
        {
            ReplicateConfiguration = ReplicateConfiguration.ToGrpc()
        };
    }
}
