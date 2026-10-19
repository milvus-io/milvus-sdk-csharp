namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to list all registered file resources.
/// </summary>
public sealed class ListFileResourcesReq
{
    internal static Grpc.ListFileResourcesRequest ToGrpcListFileResourcesRequest()
        => new();
}
