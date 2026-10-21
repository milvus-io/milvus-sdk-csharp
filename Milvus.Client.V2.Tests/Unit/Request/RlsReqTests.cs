using System.Text.Json;

using Xunit;

using Milvus.Client.V2.Requests.Rls;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class RlsReqTests
{
    [Fact]
    public void CreateRowPolicy_maps_all_fields()
    {
        var request = new CreateRowPolicyReq
        {
            DatabaseName = "db1",
            CollectionName = "book",
            PolicyName = "policy1",
            PolicyType = RowPolicyType.Restrictive,
            Actions = new[] { RowPolicyAction.Query, RowPolicyAction.Insert },
            UsingExpr = "team == $current_principal_tags['team']",
            CheckExpr = "team == $current_principal_tags['team']",
            Description = "a description"
        };

        Grpc.CreateRowPolicyRequest grpc = request.ToGrpcCreateRowPolicyRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("policy1", grpc.PolicyName);
        Assert.Equal(Grpc.RowPolicyType.Restrictive, grpc.PolicyType);
        Assert.Equal(new[] { Grpc.RowPolicyAction.Query, Grpc.RowPolicyAction.Insert }, grpc.Actions);
        Assert.Equal("team == $current_principal_tags['team']", grpc.UsingExpr);
        Assert.Equal("team == $current_principal_tags['team']", grpc.CheckExpr);
        Assert.Equal("a description", grpc.Description);
    }

    [Fact]
    public void CreateRowPolicy_defaults_to_permissive_policy_type()
    {
        var request = new CreateRowPolicyReq
        {
            CollectionName = "book",
            PolicyName = "policy1",
            Actions = new[] { RowPolicyAction.Query }
        };

        Grpc.CreateRowPolicyRequest grpc = request.ToGrpcCreateRowPolicyRequest();

        Assert.Equal(Grpc.RowPolicyType.Permissive, grpc.PolicyType);
        Assert.Equal("", grpc.DbName);
    }

    [Fact]
    public void CreateRowPolicy_rejects_missing_actions()
    {
        var request = new CreateRowPolicyReq { CollectionName = "book", PolicyName = "policy1" };

        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateRowPolicyRequest());
    }

    [Fact]
    public void CreateRowPolicy_rejects_unknown_action()
    {
        var request = new CreateRowPolicyReq
        {
            CollectionName = "book",
            PolicyName = "policy1",
            Actions = new[] { RowPolicyAction.Unknown }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateRowPolicyRequest());
    }

    [Fact]
    public void UpdateRowPolicy_maps_all_fields()
    {
        var request = new UpdateRowPolicyReq
        {
            DatabaseName = "db1",
            CollectionName = "book",
            PolicyName = "policy1",
            PolicyType = RowPolicyType.Permissive,
            Actions = new[] { RowPolicyAction.Delete },
            UsingExpr = "team == $current_principal_tags['team']",
            CheckExpr = "team == $current_principal_tags['team']",
            Description = "updated"
        };

        Grpc.UpdateRowPolicyRequest grpc = request.ToGrpcUpdateRowPolicyRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("policy1", grpc.PolicyName);
        Assert.Equal(Grpc.RowPolicyType.Permissive, grpc.PolicyType);
        Assert.Equal(new[] { Grpc.RowPolicyAction.Delete }, grpc.Actions);
        Assert.Equal("updated", grpc.Description);
    }

    [Fact]
    public void UpdateRowPolicy_requires_policy_type()
    {
        var request = new UpdateRowPolicyReq
        {
            CollectionName = "book",
            PolicyName = "policy1",
            Actions = new[] { RowPolicyAction.Delete }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcUpdateRowPolicyRequest());
    }

    [Fact]
    public void UpdateRowPolicy_rejects_unknown_action()
    {
        var request = new UpdateRowPolicyReq
        {
            CollectionName = "book",
            PolicyName = "policy1",
            PolicyType = RowPolicyType.Permissive,
            Actions = new[] { RowPolicyAction.Unknown }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcUpdateRowPolicyRequest());
    }

    [Fact]
    public void DropRowPolicy_maps_fields()
    {
        var request = new DropRowPolicyReq { DatabaseName = "db1", CollectionName = "book", PolicyName = "policy1" };

        Grpc.DropRowPolicyRequest grpc = request.ToGrpcDropRowPolicyRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("policy1", grpc.PolicyName);
    }

    [Fact]
    public void ListRowPolicies_maps_fields()
    {
        var request = new ListRowPoliciesReq { DatabaseName = "db1", CollectionName = "book" };

        Grpc.ListRowPoliciesRequest grpc = request.ToGrpcListRowPoliciesRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void SetRLSPrincipalTags_serializes_tags_to_json()
    {
        var request = new SetRLSPrincipalTagsReq
        {
            DatabaseName = "db1",
            CollectionName = "book",
            PrincipalName = "alice",
            Tags =
            {
                ["team"] = "engineering",
                ["level"] = 3,
                ["scores"] = new[] { 1.5, 2.5 }
            }
        };

        Grpc.SetRLSPrincipalTagsRequest grpc = request.ToGrpcSetRLSPrincipalTagsRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("alice", grpc.PrincipalName);

        using JsonDocument document = JsonDocument.Parse(grpc.Tags);
        Assert.Equal("engineering", document.RootElement.GetProperty("team").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("level").GetInt64());
        Assert.Equal(1.5, document.RootElement.GetProperty("scores")[0].GetDouble());
    }

    [Fact]
    public void SetRLSPrincipalTags_rejects_empty_tags()
    {
        var request = new SetRLSPrincipalTagsReq { CollectionName = "book", PrincipalName = "alice" };

        Assert.Throws<ArgumentException>(() => request.ToGrpcSetRLSPrincipalTagsRequest());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void SetRLSPrincipalTags_rejects_non_finite_number_values(object invalidValue)
    {
        var request = new SetRLSPrincipalTagsReq
        {
            CollectionName = "book",
            PrincipalName = "alice",
            Tags = { ["score"] = invalidValue }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcSetRLSPrincipalTagsRequest());
    }

    [Fact]
    public void SetRLSPrincipalTags_rejects_nested_collection_values()
    {
        var request = new SetRLSPrincipalTagsReq
        {
            CollectionName = "book",
            PrincipalName = "alice",
            Tags = { ["nested"] = new[] { new[] { 1, 2 } } }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcSetRLSPrincipalTagsRequest());
    }

    [Fact]
    public void SetRLSPrincipalTags_rejects_non_string_non_number_values()
    {
        var request = new SetRLSPrincipalTagsReq
        {
            CollectionName = "book",
            PrincipalName = "alice",
            Tags = { ["bad"] = true }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcSetRLSPrincipalTagsRequest());
    }

    [Fact]
    public void SetRLSPrincipalTags_rejects_big_integer_values()
    {
        // BigInteger passes the numeric family but cannot be serialized by System.Text.Json on the SDK's
        // target frameworks; it must be rejected up front instead of failing later in JsonSerializer.Serialize.
        var request = new SetRLSPrincipalTagsReq
        {
            CollectionName = "book",
            PrincipalName = "alice",
            Tags = { ["big"] = System.Numerics.BigInteger.Parse(
                "123456789012345678901234567890", System.Globalization.CultureInfo.InvariantCulture) }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcSetRLSPrincipalTagsRequest());
    }

    [Fact]
    public void GetRLSPrincipalTags_maps_fields()
    {
        var request = new GetRLSPrincipalTagsReq { DatabaseName = "db1", CollectionName = "book", PrincipalName = "alice" };

        Grpc.GetRLSPrincipalTagsRequest grpc = request.ToGrpcGetRLSPrincipalTagsRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("alice", grpc.PrincipalName);
    }

    [Fact]
    public void ListRLSPrincipals_maps_fields()
    {
        var request = new ListRLSPrincipalsReq { DatabaseName = "db1", CollectionName = "book" };

        Grpc.ListRLSPrincipalsRequest grpc = request.ToGrpcListRLSPrincipalsRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void DeleteRLSPrincipalTags_maps_tag_keys()
    {
        var request = new DeleteRLSPrincipalTagsReq
        {
            DatabaseName = "db1",
            CollectionName = "book",
            PrincipalName = "alice",
            TagKeys = new[] { "level", "team" }
        };

        Grpc.DeleteRLSPrincipalTagsRequest grpc = request.ToGrpcDeleteRLSPrincipalTagsRequest();

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("alice", grpc.PrincipalName);
        Assert.Equal(new[] { "level", "team" }, grpc.TagKeys);
    }

    [Fact]
    public void DeleteRLSPrincipalTags_without_tag_keys_leaves_list_empty()
    {
        var request = new DeleteRLSPrincipalTagsReq { CollectionName = "book", PrincipalName = "alice" };

        Grpc.DeleteRLSPrincipalTagsRequest grpc = request.ToGrpcDeleteRLSPrincipalTagsRequest();

        Assert.Empty(grpc.TagKeys);
    }
}
