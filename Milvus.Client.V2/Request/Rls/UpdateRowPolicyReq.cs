using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to update an existing row policy for a collection.
/// </summary>
/// <remarks>
/// The policy must exist. All definition fields replace their old values; omitted expressions and description
/// become empty strings.
/// </remarks>
public sealed class UpdateRowPolicyReq
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
    /// The name of the row policy to update.
    /// </summary>
    public string PolicyName { get; set; } = "";

    /// <summary>
    /// The type of the row policy: permissive or restrictive. Must be set explicitly: unlike
    /// <see cref="CreateRowPolicyReq" />, the update wire contract carries no "omitted" policy type, and the
    /// server rejects an invalid type rather than defaulting it, so an unset value fails fast instead of
    /// silently re-typing the policy (e.g. re-typing a Restrictive policy to Permissive).
    /// </summary>
    public RowPolicyType? PolicyType { get; set; }

    /// <summary>
    /// The actions governed by the policy.
    /// </summary>
    public IReadOnlyList<RowPolicyAction> Actions { get; set; } = Array.Empty<RowPolicyAction>();

    /// <summary>
    /// The expression used to filter rows visible to a principal.
    /// </summary>
    public string UsingExpr { get; set; } = "";

    /// <summary>
    /// The expression used to validate rows written by a principal.
    /// </summary>
    public string CheckExpr { get; set; } = "";

    /// <summary>
    /// The policy description.
    /// </summary>
    public string Description { get; set; } = "";

    internal Grpc.UpdateRowPolicyRequest ToGrpcUpdateRowPolicyRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(PolicyName);
        Verify.NotNullOrEmpty(Actions);

        foreach (RowPolicyAction action in Actions)
        {
            // RowPolicyAction.Unknown (-1) is a response-only sentinel; sending it on the wire would cast to
            // the proto UNRECOGNIZED value, so reject it (and any other out-of-range value) up front.
            if (action is < RowPolicyAction.Query or > RowPolicyAction.HybridSearch)
            {
                throw new ArgumentException(
                    $"Action '{action}' is not a valid row policy action; Unknown is response-only.", nameof(Actions));
            }
        }

        if (PolicyType is null)
        {
            // The update proto's policy_type field is not optional, and the server rejects an invalid type
            // instead of defaulting it (unlike create, which treats an omitted type as Permissive). Requiring
            // it explicitly prevents a caller from silently re-typing the policy.
            throw new ArgumentException("PolicyType must be specified for a row policy update.", nameof(PolicyType));
        }

        var request = new Grpc.UpdateRowPolicyRequest
        {
            CollectionName = CollectionName,
            PolicyName = PolicyName,
            PolicyType = (Grpc.RowPolicyType)(int)PolicyType.Value,
            UsingExpr = UsingExpr,
            CheckExpr = CheckExpr,
            Description = Description
        };
        request.Actions.AddRange(Actions.Select(a => (Grpc.RowPolicyAction)(int)a));
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
