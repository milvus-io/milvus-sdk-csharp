using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Rls;

/// <summary>
/// The definition of a single row policy.
/// </summary>
public sealed class RowPolicyInfo
{
    internal RowPolicyInfo(
        string policyName,
        RowPolicyType policyType,
        IReadOnlyList<RowPolicyAction> actions,
        string usingExpr,
        string checkExpr,
        string description)
    {
        PolicyName = policyName;
        PolicyType = policyType;
        Actions = actions;
        UsingExpr = usingExpr;
        CheckExpr = checkExpr;
        Description = description;
    }

    internal static RowPolicyInfo FromGrpc(Grpc.RowPolicy policy)
        => new(
            policy.PolicyName,
            MapPolicyType(policy.PolicyType),
            policy.Actions.Select(MapAction).ToList(),
            policy.UsingExpr,
            policy.CheckExpr,
            policy.Description);

    // Guard unrecognized proto values exactly like MapAction so a future or unrecognized policy-type code
    // (e.g. proto3 UNRECOGNIZED = -1, or a value added by a newer server than this proto submodule) surfaces
    // as RowPolicyType.Unknown instead of an out-of-range numeric enum value callers cannot pattern-match.
    private static RowPolicyType MapPolicyType(Grpc.RowPolicyType policyType)
        => Enum.IsDefined(typeof(Grpc.RowPolicyType), (int)policyType)
            ? (RowPolicyType)(int)policyType
            : RowPolicyType.Unknown;

    private static RowPolicyAction MapAction(Grpc.RowPolicyAction action)
        => Enum.IsDefined(typeof(Grpc.RowPolicyAction), (int)action)
            ? (RowPolicyAction)(int)action
            : RowPolicyAction.Unknown;

    /// <summary>
    /// The name of the row policy.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// The type of the row policy: permissive or restrictive.
    /// </summary>
    public RowPolicyType PolicyType { get; }

    /// <summary>
    /// The actions governed by the policy.
    /// </summary>
    public IReadOnlyList<RowPolicyAction> Actions { get; }

    /// <summary>
    /// The expression used to filter rows visible to a principal.
    /// </summary>
    public string UsingExpr { get; }

    /// <summary>
    /// The expression used to validate rows written by a principal.
    /// </summary>
    public string CheckExpr { get; }

    /// <summary>
    /// The policy description.
    /// </summary>
    public string Description { get; }
}
