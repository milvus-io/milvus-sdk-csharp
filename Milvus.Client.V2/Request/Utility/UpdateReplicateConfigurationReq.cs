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

    /// <summary>
    /// When set, the current cluster is promoted to primary forcefully. Intended for failover scenarios.
    /// </summary>
    public bool ForcePromote { get; set; }

    internal Grpc.UpdateReplicateConfigurationRequest ToGrpcRequest()
    {
        Verify.NotNull(ReplicateConfiguration);

        return new Grpc.UpdateReplicateConfigurationRequest
        {
            ReplicateConfiguration = ReplicateConfiguration.ToGrpc(),
            ForcePromote = ForcePromote
        };
    }
}
