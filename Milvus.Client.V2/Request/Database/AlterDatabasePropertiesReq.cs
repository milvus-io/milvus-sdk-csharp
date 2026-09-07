using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Database;

/// <summary>
/// Represents a request to alter the properties of a database.
/// </summary>
public sealed class AlterDatabasePropertiesReq
{
    /// <summary>
    /// The name of the database.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>
    /// The properties to set or update on the database.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The names of the properties to remove from the database.
    /// </summary>
    public IReadOnlyList<string>? DeleteKeys { get; set; }

    internal Grpc.AlterDatabaseRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(DatabaseName);

        var request = new Grpc.AlterDatabaseRequest { DbName = DatabaseName };
        foreach (KeyValuePair<string, string> property in Properties)
        {
            request.Properties.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }

        if (DeleteKeys is not null)
        {
            request.DeleteKeys.AddRange(DeleteKeys);
        }

        return request;
    }
}
