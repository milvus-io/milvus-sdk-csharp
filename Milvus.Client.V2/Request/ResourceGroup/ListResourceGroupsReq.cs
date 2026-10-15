namespace Milvus.Client.V2.Requests.ResourceGroup;

/// <summary>
/// Represents a request to list all resource groups.
/// </summary>
public sealed class ListResourceGroupsReq
{
    internal static Grpc.ListResourceGroupsRequest ToGrpcListResourceGroupsRequest() => new();
}
