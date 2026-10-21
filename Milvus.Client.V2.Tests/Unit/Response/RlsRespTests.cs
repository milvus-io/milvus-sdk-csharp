using Xunit;

using Grpc = Milvus.Client.Grpc;
using Milvus.Client.V2.Responses.Rls;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Response;

[Trait("Category", "Unit")]
public class RlsRespTests
{
    [Fact]
    public void RowPolicyInfo_maps_all_fields()
    {
        var policy = new Grpc.RowPolicy
        {
            PolicyName = "policy1",
            PolicyType = Grpc.RowPolicyType.Permissive,
            UsingExpr = "team == $current_principal_tags['team']",
            CheckExpr = "team == $current_principal_tags['team']",
            Description = "a description"
        };
        policy.Actions.Add(Grpc.RowPolicyAction.Search);
        policy.Actions.Add(Grpc.RowPolicyAction.Insert);

        RowPolicyInfo info = RowPolicyInfo.FromGrpc(policy);

        Assert.Equal("policy1", info.PolicyName);
        Assert.Equal(RowPolicyType.Permissive, info.PolicyType);
        Assert.Equal(new[] { RowPolicyAction.Search, RowPolicyAction.Insert }, info.Actions);
        Assert.Equal("team == $current_principal_tags['team']", info.UsingExpr);
        Assert.Equal("team == $current_principal_tags['team']", info.CheckExpr);
        Assert.Equal("a description", info.Description);
    }

    [Fact]
    public void RowPolicyInfo_maps_unrecognized_policy_type_to_unknown()
    {
        var policy = new Grpc.RowPolicy { PolicyName = "policy1", PolicyType = (Grpc.RowPolicyType)99 };

        RowPolicyInfo info = RowPolicyInfo.FromGrpc(policy);

        Assert.Equal(RowPolicyType.Unknown, info.PolicyType);
    }

    [Fact]
    public void RowPolicyInfo_maps_unrecognized_action_to_unknown()
    {
        var policy = new Grpc.RowPolicy { PolicyName = "policy1", PolicyType = Grpc.RowPolicyType.Restrictive };
        policy.Actions.Add((Grpc.RowPolicyAction)99);

        RowPolicyInfo info = RowPolicyInfo.FromGrpc(policy);

        Assert.Equal(new[] { RowPolicyAction.Unknown }, info.Actions);
    }

    [Fact]
    public void ListRowPoliciesResp_maps_policies()
    {
        var response = new Grpc.ListRowPoliciesResponse
        {
            Status = new Grpc.Status { Code = 0 },
            DbName = "db1",
            CollectionName = "book"
        };
        response.Policies.Add(new Grpc.RowPolicy { PolicyName = "policy1", PolicyType = Grpc.RowPolicyType.Restrictive });

        ListRowPoliciesResp result = ListRowPoliciesResp.FromGrpc(response);

        Assert.Equal("db1", result.DbName);
        Assert.Equal("book", result.CollectionName);
        RowPolicyInfo policy = Assert.Single(result.Policies);
        Assert.Equal("policy1", policy.PolicyName);
        Assert.Equal(RowPolicyType.Restrictive, policy.PolicyType);
    }

    [Fact]
    public void ListRLSPrincipalsResp_maps_principal_names()
    {
        var response = new Grpc.ListRLSPrincipalsResponse { Status = new Grpc.Status { Code = 0 } };
        response.PrincipalNames.AddRange(new[] { "alice", "bob" });

        ListRLSPrincipalsResp result = ListRLSPrincipalsResp.FromGrpc(response);

        Assert.Equal(new[] { "alice", "bob" }, result.PrincipalNames);
    }

    [Fact]
    public void GetRLSPrincipalTagsResp_decodes_tags()
    {
        var response = new Grpc.GetRLSPrincipalTagsResponse
        {
            Status = new Grpc.Status { Code = 0 },
            Tags = """{"team":"engineering","level":3,"active":true,"roles":["admin","dev"]}"""
        };

        GetRLSPrincipalTagsResp result = GetRLSPrincipalTagsResp.FromGrpc(response);

        Assert.Equal("engineering", result.Tags["team"]);
        Assert.Equal(3L, result.Tags["level"]);
        Assert.Equal(true, result.Tags["active"]);
        Assert.Equal(new object?[] { "admin", "dev" }, result.Tags["roles"]);
    }

    [Fact]
    public void GetRLSPrincipalTagsResp_returns_empty_tags_when_response_empty()
    {
        var response = new Grpc.GetRLSPrincipalTagsResponse { Status = new Grpc.Status { Code = 0 }, Tags = "" };

        GetRLSPrincipalTagsResp result = GetRLSPrincipalTagsResp.FromGrpc(response);

        Assert.Empty(result.Tags);
    }

    [Fact]
    public void GetRLSPrincipalTagsResp_throws_on_non_object_tags()
    {
        var response = new Grpc.GetRLSPrincipalTagsResponse
        {
            Status = new Grpc.Status { Code = 0 },
            Tags = """["a","b"]"""
        };

        MilvusException exception = Assert.Throws<MilvusException>(() => GetRLSPrincipalTagsResp.FromGrpc(response));
        Assert.Contains("must be a JSON object", exception.Message);
    }
}
