using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Aliases;

/// <summary>
/// Represents a request to alter an alias so that it points to a different collection.
/// </summary>
public sealed class AlterAliasReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection the alias should point to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the alias to alter.
    /// </summary>
    public string Alias { get; set; } = "";
    internal Grpc.AlterAliasRequest ToGrpcAlterAliasRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(Alias);
        var request = new Grpc.AlterAliasRequest { CollectionName = CollectionName, Alias = Alias };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
