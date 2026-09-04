using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to alter the properties of a collection.
/// </summary>
public sealed class AlterCollectionPropertiesReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The properties to set or update on the collection.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The names of properties to remove from the collection.
    /// </summary>
    public IReadOnlyList<string>? DeleteKeys { get; set; }

    internal Grpc.AlterCollectionRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        if (Properties.Count == 0 && DeleteKeys is not { Count: > 0 })
        {
            throw new ArgumentException("At least one of Properties or DeleteKeys must be provided.", nameof(Properties));
        }

        var request = new Grpc.AlterCollectionRequest { CollectionName = CollectionName };
        foreach (KeyValuePair<string, string> property in Properties)
        {
            request.Properties.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }

        if (DeleteKeys is not null)
        {
            request.DeleteKeys.AddRange(DeleteKeys);
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
