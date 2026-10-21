using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to list the principals that have stored tags in a collection.
/// </summary>
public sealed class ListRLSPrincipalsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to list the RLS principals of.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.ListRLSPrincipalsRequest ToGrpcListRLSPrincipalsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.ListRLSPrincipalsRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
