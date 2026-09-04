#pragma warning disable CS1591 // Missing XML docs

using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Database;
public sealed class CreateDatabaseReq
{
    public string DatabaseName { get; set; } = "";
    internal Grpc.CreateDatabaseRequest ToGrpcCreateDatabaseRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        return new Grpc.CreateDatabaseRequest { DbName = DatabaseName };
    }
}
