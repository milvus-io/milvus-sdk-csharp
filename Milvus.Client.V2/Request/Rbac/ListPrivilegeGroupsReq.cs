namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to list all privilege groups.
/// </summary>
public sealed class ListPrivilegeGroupsReq
{
    internal static Grpc.ListPrivilegeGroupsRequest ToGrpcListPrivilegeGroupsRequest() => new();
}
