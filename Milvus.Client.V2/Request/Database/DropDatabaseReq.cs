using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Database;

/// <summary>
/// Represents a request to drop a database.
/// </summary>
public sealed class DropDatabaseReq
{
    /// <summary>
    /// The name of the database to drop.
    /// </summary>
    public string DatabaseName { get; set; } = "";
    internal Grpc.DropDatabaseRequest ToGrpcDropDatabaseRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        return new Grpc.DropDatabaseRequest { DbName = DatabaseName };
    }
}
