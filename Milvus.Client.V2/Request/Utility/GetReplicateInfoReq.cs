using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get replication info about a physical channel.
/// </summary>
public sealed class GetReplicateInfoReq
{
    /// <summary>
    /// The ID of the source cluster.
    /// </summary>
    public string SourceClusterId { get; set; } = "";

    /// <summary>
    /// The target physical channel.
    /// </summary>
    public string TargetPchannel { get; set; } = "";

    internal Grpc.GetReplicateInfoRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(SourceClusterId);
        Verify.NotNullOrWhiteSpace(TargetPchannel);

        return new Grpc.GetReplicateInfoRequest
        {
            SourceClusterId = SourceClusterId,
            TargetPchannel = TargetPchannel
        };
    }
}
