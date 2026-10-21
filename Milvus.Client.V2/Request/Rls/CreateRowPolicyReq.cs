using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Rls;

/// <summary>
/// Represents a request to create a row policy for a collection.
/// </summary>
/// <remarks>
/// The policy type accepts permissive or restrictive, and the actions control which operations the policy
/// applies to. The expressions are validated by Milvus. An existing policy name is an error.
/// </remarks>
public sealed class CreateRowPolicyReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to create the row policy for.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the row policy to create.
    /// </summary>
    public string PolicyName { get; set; } = "";

    /// <summary>
    /// The type of the row policy: permissive or restrictive. Defaults to
    /// <see cref="RowPolicyType.Permissive" />, matching the server's default for an omitted value.
    /// </summary>
    public RowPolicyType PolicyType { get; set; } = RowPolicyType.Permissive;

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

    internal Grpc.CreateRowPolicyRequest ToGrpcCreateRowPolicyRequest()
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

        var request = new Grpc.CreateRowPolicyRequest
        {
            CollectionName = CollectionName,
            PolicyName = PolicyName,
            PolicyType = (Grpc.RowPolicyType)(int)PolicyType,
            UsingExpr = UsingExpr,
            CheckExpr = CheckExpr,
            Description = Description
        };
        request.Actions.AddRange(Actions.Select(a => (Grpc.RowPolicyAction)(int)a));
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
