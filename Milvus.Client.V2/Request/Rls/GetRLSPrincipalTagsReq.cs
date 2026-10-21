using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to get the tags of a principal for a collection.
/// </summary>
public sealed class GetRLSPrincipalTagsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection the principal tags belong to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the principal whose tags are retrieved.
    /// </summary>
    public string PrincipalName { get; set; } = "";

    internal Grpc.GetRLSPrincipalTagsRequest ToGrpcGetRLSPrincipalTagsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PrincipalName);

        var request = new Grpc.GetRLSPrincipalTagsRequest
        {
            CollectionName = CollectionName,
            PrincipalName = PrincipalName
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
