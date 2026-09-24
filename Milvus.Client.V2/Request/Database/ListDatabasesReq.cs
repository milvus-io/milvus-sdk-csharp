namespace Milvus.Client.V2.Requests.Database;

/// <summary>
/// Represents a request to list all databases.
/// </summary>
public sealed class ListDatabasesReq
{
    internal static Grpc.ListDatabasesRequest ToGrpcListDatabasesRequest() => new();
}
