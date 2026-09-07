#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Responses.Database;
public sealed class ListDatabasesResp
{
    private ListDatabasesResp(IReadOnlyList<string> databaseNames)
    {
        DatabaseNames = databaseNames;
    }
    internal static ListDatabasesResp FromGrpc(Grpc.ListDatabasesResponse response)
        => new(response.DbNames.ToList());
    public IReadOnlyList<string> DatabaseNames { get; }
}
