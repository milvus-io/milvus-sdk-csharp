using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to alter the properties of an index.
/// </summary>
public sealed class AlterIndexPropertiesReq
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
    /// The index name. Defaults to <c>"_default_idx"</c> when unset (the server's default index name, also
    /// used by <c>CreateIndexReq</c>); the proxy rejects an empty index name on alter.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// The properties to set or update on the index.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The names of the properties to remove from the index.
    /// </summary>
    public IReadOnlyList<string>? DeleteKeys { get; set; }

    internal Grpc.AlterIndexRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        if (Properties.Count == 0 && DeleteKeys is not { Count: > 0 })
        {
            throw new ArgumentException("At least one of Properties or DeleteKeys must be provided.", nameof(Properties));
        }

        // The proxy's alter index task rejects a request carrying both DeleteKeys and ExtraParams.
        if (Properties.Count > 0 && DeleteKeys is { Count: > 0 })
        {
            throw new ArgumentException("Properties and DeleteKeys are mutually exclusive.", nameof(Properties));
        }

        var request = new Grpc.AlterIndexRequest
        {
            CollectionName = CollectionName,
            IndexName = string.IsNullOrEmpty(IndexName) ? Constants.DefaultIndexName : IndexName
        };
        foreach (KeyValuePair<string, string> property in Properties)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair { Key = property.Key, Value = property.Value });
        }

        if (DeleteKeys is not null)
        {
            request.DeleteKeys.AddRange(DeleteKeys);
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
