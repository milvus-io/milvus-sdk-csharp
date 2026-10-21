using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to delete the selected tag keys of a principal, or all tags when the tag key list is
/// empty.
/// </summary>
public sealed class DeleteRLSPrincipalTagsReq
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
    /// The name of the principal whose tags are deleted.
    /// </summary>
    public string PrincipalName { get; set; } = "";

    /// <summary>
    /// The tag keys to delete. An empty or null list deletes all tags of the principal.
    /// </summary>
    public IReadOnlyList<string>? TagKeys { get; set; }

    internal Grpc.DeleteRLSPrincipalTagsRequest ToGrpcDeleteRLSPrincipalTagsRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PrincipalName);

        var request = new Grpc.DeleteRLSPrincipalTagsRequest
        {
            CollectionName = CollectionName,
            PrincipalName = PrincipalName
        };
        if (TagKeys is not null)
        {
            request.TagKeys.AddRange(TagKeys);
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
