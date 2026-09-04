namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of an update-replicate-configuration operation.
/// </summary>
public sealed class UpdateReplicateConfigurationResp
{
    internal UpdateReplicateConfigurationResp()
    {
    }

    internal static UpdateReplicateConfigurationResp FromGrpc() => new();
}
