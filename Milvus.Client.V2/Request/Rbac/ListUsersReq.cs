#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.Rbac;
public sealed class ListUsersReq
{
    internal static Grpc.ListCredUsersRequest ToGrpcListCredUsersRequest() => new();
}
