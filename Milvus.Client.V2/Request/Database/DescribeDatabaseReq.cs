#pragma warning disable CS1591 // Missing XML docs
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Database;
public sealed class DescribeDatabaseReq
{
    public string DatabaseName { get; set; } = "";
    internal Grpc.DescribeDatabaseRequest ToGrpcDescribeDatabaseRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        return new Grpc.DescribeDatabaseRequest { DbName = DatabaseName };
    }
}
