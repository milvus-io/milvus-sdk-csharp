namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to list all users.
/// </summary>
public sealed class ListUsersReq
{
    internal static Grpc.ListCredUsersRequest ToGrpcListCredUsersRequest() => new();
}
