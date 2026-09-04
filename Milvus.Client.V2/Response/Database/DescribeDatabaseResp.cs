#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.Database;
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
    public string DatabaseName { get; }
    public long DbId { get; }
    public ulong CreatedTimestamp { get; }
    public IReadOnlyDictionary<string, string> Properties { get; }
}
