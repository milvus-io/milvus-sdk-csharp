using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of a get-replicate-configuration operation.
/// </summary>
public sealed class GetReplicateConfigurationResp
{
    private GetReplicateConfigurationResp(ReplicateConfiguration configuration)
    {
        Configuration = configuration;
    }

    internal static GetReplicateConfigurationResp FromGrpc(Grpc.GetReplicateConfigurationResponse response)
        => new(response.Configuration is null ? new ReplicateConfiguration([]) : ReplicateConfiguration.FromGrpc(response.Configuration));

    /// <summary>
    /// The current replication configuration of the cluster.
    /// </summary>
    public ReplicateConfiguration Configuration { get; }
}
