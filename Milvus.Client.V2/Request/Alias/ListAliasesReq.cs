using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Aliases;

/// <summary>
/// Represents a request to list the aliases of a collection, or of all collections when <see cref="CollectionName"/>
/// is not set.
/// </summary>
public sealed class ListAliasesReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection whose aliases to list. When <c>null</c>, the aliases of all collections are
    /// returned.
    /// </summary>
    public string? CollectionName { get; set; }
    internal Grpc.ListAliasesRequest ToGrpcListAliasesRequest()
        => new()
        {
            DbName = DatabaseName ?? "",
            CollectionName = CollectionName ?? ""
        };
}
