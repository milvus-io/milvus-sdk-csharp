using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Database;

/// <summary>
/// Represents a request to drop the properties of a database.
/// </summary>
public sealed class DropDatabasePropertiesReq
{
    /// <summary>
    /// The name of the database.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>
    /// The names of the properties to remove from the database.
    /// </summary>
    public IReadOnlyList<string> DeleteKeys { get; set; } = Array.Empty<string>();

    internal Grpc.AlterDatabaseRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        Verify.NotNullOrEmpty(DeleteKeys);

        var request = new Grpc.AlterDatabaseRequest { DbName = DatabaseName };
        request.DeleteKeys.AddRange(DeleteKeys);
        return request;
    }
}
