namespace Milvus.Client.V2.Responses.Database;

/// <summary>
/// The result of a describe-database request.
/// </summary>
public sealed class DescribeDatabaseResp
{
    internal DescribeDatabaseResp(string databaseName, long dbId, ulong createdTimestamp, IReadOnlyDictionary<string, string> properties)
    {
        DatabaseName = databaseName;
        DbId = dbId;
        CreatedTimestamp = createdTimestamp;
        Properties = properties;
    }
    internal static DescribeDatabaseResp FromGrpc(Grpc.DescribeDatabaseResponse response)
    {
        var properties = new Dictionary<string, string>();
        foreach (Grpc.KeyValuePair property in response.Properties)
        {
            properties[property.Key] = property.Value;
        }
        return new DescribeDatabaseResp(response.DbName, response.DbID, response.CreatedTimestamp, properties);
    }

    /// <summary>
    /// The name of the database.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// The ID of the database.
    /// </summary>
    public long DbId { get; }

    /// <summary>
    /// The timestamp at which the database was created.
    /// </summary>
    public ulong CreatedTimestamp { get; }

    /// <summary>
    /// The properties of the database, keyed by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; }
}
