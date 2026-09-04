#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Database;
public sealed class DropDatabaseReq
{
    public string DatabaseName { get; set; } = "";
    internal Grpc.DropDatabaseRequest ToGrpcDropDatabaseRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        return new Grpc.DropDatabaseRequest { DbName = DatabaseName };
    }
}
