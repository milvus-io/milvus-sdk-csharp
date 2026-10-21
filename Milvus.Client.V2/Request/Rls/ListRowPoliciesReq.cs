using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to list the row policies of a collection.
/// </summary>
public sealed class ListRowPoliciesReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to list the row policies of.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.ListRowPoliciesRequest ToGrpcListRowPoliciesRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.ListRowPoliciesRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
