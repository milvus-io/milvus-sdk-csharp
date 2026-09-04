#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Requests.Database;
public sealed class ListDatabasesReq
{
    internal static Grpc.ListDatabasesRequest ToGrpcListDatabasesRequest() => new();
}
