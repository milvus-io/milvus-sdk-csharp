using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Database;

/// <summary>
/// Represents a request to describe a database.
/// </summary>
public sealed class DescribeDatabaseReq
{
    /// <summary>
    /// The name of the database to describe.
    /// </summary>
    public string DatabaseName { get; set; } = "";
    internal Grpc.DescribeDatabaseRequest ToGrpcDescribeDatabaseRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        return new Grpc.DescribeDatabaseRequest { DbName = DatabaseName };
    }
}
