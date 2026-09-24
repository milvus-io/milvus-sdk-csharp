using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Database;

/// <summary>
/// Represents a request to create a database.
/// </summary>
public sealed class CreateDatabaseReq
{
    /// <summary>
    /// The name of the database to create.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>
    /// Database-level properties applied at creation time.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    internal Grpc.CreateDatabaseRequest ToGrpcCreateDatabaseRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);
        var request = new Grpc.CreateDatabaseRequest { DbName = DatabaseName };
        foreach (KeyValuePair<string, string> property in Properties)
        {
            request.Properties.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }

        return request;
    }
}
