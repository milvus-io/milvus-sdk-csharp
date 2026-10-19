namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a <c>ListFileResources</c> operation.
/// </summary>
public sealed class ListFileResourcesResp
{
    private ListFileResourcesResp(IReadOnlyList<FileResourceInfo> resources) => Resources = resources;

    internal static ListFileResourcesResp FromGrpc(Grpc.ListFileResourcesResponse response)
        => new(response.Resources.Select(FileResourceInfo.FromGrpc).ToList());

    /// <summary>
    /// The registered file resources.
    /// </summary>
    public IReadOnlyList<FileResourceInfo> Resources { get; }
}
