namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to list all collections in the database.
/// </summary>
public sealed class ListCollectionsReq
{
    internal static Grpc.ShowCollectionsRequest ToGrpcShowCollectionsRequest()
        => new();
}
