namespace Milvus.Client.V2.Requests.Rbac;

/// <summary>
/// Represents a request to list all roles.
/// </summary>
public sealed class ListRolesReq
{
    internal static Grpc.SelectRoleRequest ToGrpcSelectRoleRequest()
        => new() { IncludeUserInfo = true };
}
