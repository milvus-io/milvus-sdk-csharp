using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to drop a row policy. Dropping a missing policy succeeds.
/// </summary>
public sealed class DropRowPolicyReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection the row policy belongs to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the row policy to drop.
    /// </summary>
    public string PolicyName { get; set; } = "";

    internal Grpc.DropRowPolicyRequest ToGrpcDropRowPolicyRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PolicyName);

        var request = new Grpc.DropRowPolicyRequest
        {
            CollectionName = CollectionName,
            PolicyName = PolicyName
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
