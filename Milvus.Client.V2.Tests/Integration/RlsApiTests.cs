using System.Text.Json;

using Xunit;

using Grpc = Milvus.Client.Grpc;
using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Rls;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Responses.Rls;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class RlsApiTests
{
    [Fact]
    public async Task CreateRowPolicy_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateRowPolicyAsync(
            new CreateRowPolicyReq
            {
                DatabaseName = "db1",
                CollectionName = "book",
                PolicyName = "policy1",
                PolicyType = RowPolicyType.Permissive,
                Actions = new[] { RowPolicyAction.Search, RowPolicyAction.Query, RowPolicyAction.Insert },
                UsingExpr = "team == $current_principal_tags['team']",
                CheckExpr = "team == $current_principal_tags['team']",
                Description = "a description"
            },
            TestContext.Current.CancellationToken);

        Grpc.CreateRowPolicyRequest request = Assert.IsType<Grpc.CreateRowPolicyRequest>(server.Service.Requests["CreateRowPolicy"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("book", request.CollectionName);
        Assert.Equal("policy1", request.PolicyName);
        Assert.Equal(Grpc.RowPolicyAction.Search, request.Actions[0]);
        Assert.Equal(Grpc.RowPolicyAction.Query, request.Actions[1]);
        Assert.Equal(Grpc.RowPolicyAction.Insert, request.Actions[2]);
        Assert.Equal("team == $current_principal_tags['team']", request.UsingExpr);
        Assert.Equal("team == $current_principal_tags['team']", request.CheckExpr);
        Assert.Equal("a description", request.Description);
        Assert.Equal(Grpc.RowPolicyType.Permissive, request.PolicyType);
    }

    [Fact]
    public async Task UpdateRowPolicy_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UpdateRowPolicyAsync(
            new UpdateRowPolicyReq
            {
                DatabaseName = "db1",
                CollectionName = "book",
                PolicyName = "policy1",
                PolicyType = RowPolicyType.Restrictive,
                Actions = new[] { RowPolicyAction.Delete },
                UsingExpr = "team == $current_principal_tags['team']",
                CheckExpr = "team == $current_principal_tags['team']",
                Description = "updated"
            },
            TestContext.Current.CancellationToken);

        Grpc.UpdateRowPolicyRequest request = Assert.IsType<Grpc.UpdateRowPolicyRequest>(server.Service.Requests["UpdateRowPolicy"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("book", request.CollectionName);
        Assert.Equal("policy1", request.PolicyName);
        Assert.Equal(Grpc.RowPolicyAction.Delete, request.Actions[0]);
        Assert.Equal(Grpc.RowPolicyType.Restrictive, request.PolicyType);
        Assert.Equal("updated", request.Description);
    }

    [Fact]
    public async Task DropRowPolicy_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropRowPolicyAsync(
            new DropRowPolicyReq { DatabaseName = "db1", CollectionName = "book", PolicyName = "policy1" },
            TestContext.Current.CancellationToken);

        Grpc.DropRowPolicyRequest request = Assert.IsType<Grpc.DropRowPolicyRequest>(server.Service.Requests["DropRowPolicy"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("book", request.CollectionName);
        Assert.Equal("policy1", request.PolicyName);
    }

    [Fact]
    public async Task ListRowPolicies_returns_decoded_policies()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var grpcPolicy = new Grpc.RowPolicy
        {
            PolicyName = "policy1",
            PolicyType = Grpc.RowPolicyType.Permissive,
            UsingExpr = "team == $current_principal_tags['team']",
            CheckExpr = "team == $current_principal_tags['team']",
            Description = "a description"
        };
        grpcPolicy.Actions.Add(Grpc.RowPolicyAction.Search);
        grpcPolicy.Actions.Add(Grpc.RowPolicyAction.Insert);
        server.Service.RowPolicies.Add(grpcPolicy);

        ListRowPoliciesResp response = await client.ListRowPoliciesAsync(
            new ListRowPoliciesReq { DatabaseName = "db1", CollectionName = "book" },
            TestContext.Current.CancellationToken);

        Assert.IsType<Grpc.ListRowPoliciesRequest>(server.Service.Requests["ListRowPolicies"]);
        Assert.Equal("db1", response.DbName);
        Assert.Equal("book", response.CollectionName);
        RowPolicyInfo policy = Assert.Single(response.Policies);
        Assert.Equal("policy1", policy.PolicyName);
        Assert.Equal(RowPolicyType.Permissive, policy.PolicyType);
        Assert.Equal(new[] { RowPolicyAction.Search, RowPolicyAction.Insert }, policy.Actions);
        Assert.Equal("team == $current_principal_tags['team']", policy.UsingExpr);
        Assert.Equal("team == $current_principal_tags['team']", policy.CheckExpr);
        Assert.Equal("a description", policy.Description);
    }

    [Fact]
    public async Task SetRLSPrincipalTags_serializes_tags_to_json()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SetRLSPrincipalTagsAsync(
            new SetRLSPrincipalTagsReq
            {
                DatabaseName = "db1",
                CollectionName = "book",
                PrincipalName = "alice",
                Tags =
                {
                    ["team"] = "engineering",
                    ["level"] = 3
                }
            },
            TestContext.Current.CancellationToken);

        Grpc.SetRLSPrincipalTagsRequest request = Assert.IsType<Grpc.SetRLSPrincipalTagsRequest>(server.Service.Requests["SetRLSPrincipalTags"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("book", request.CollectionName);
        Assert.Equal("alice", request.PrincipalName);

        using JsonDocument document = JsonDocument.Parse(request.Tags);
        Assert.Equal("engineering", document.RootElement.GetProperty("team").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("level").GetInt64());
    }

    [Fact]
    public async Task SetRLSPrincipalTags_rejects_empty_tags()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var request = new SetRLSPrincipalTagsReq
        {
            CollectionName = "book",
            PrincipalName = "alice"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SetRLSPrincipalTagsAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRLSPrincipalTags_rejects_non_finite_number_values()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var request = new SetRLSPrincipalTagsReq
        {
            CollectionName = "book",
            PrincipalName = "alice",
            Tags = { ["score"] = double.NaN }
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.SetRLSPrincipalTagsAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRLSPrincipalTags_decodes_tags()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        server.Service.PrincipalTagsJson = """{"team":"engineering","level":3,"active":true,"roles":["admin","dev"]}""";

        GetRLSPrincipalTagsResp response = await client.GetRLSPrincipalTagsAsync(
            new GetRLSPrincipalTagsReq { DatabaseName = "db1", CollectionName = "book", PrincipalName = "alice" },
            TestContext.Current.CancellationToken);

        Grpc.GetRLSPrincipalTagsRequest request = Assert.IsType<Grpc.GetRLSPrincipalTagsRequest>(server.Service.Requests["GetRLSPrincipalTags"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("alice", request.PrincipalName);
        Assert.Equal("engineering", response.Tags["team"]);
        Assert.Equal(3L, response.Tags["level"]);
        Assert.Equal(true, response.Tags["active"]);
        Assert.Equal(new object?[] { "admin", "dev" }, response.Tags["roles"]);
    }

    [Fact]
    public async Task GetRLSPrincipalTags_returns_empty_tags_when_response_is_empty()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        server.Service.PrincipalTagsJson = "";

        GetRLSPrincipalTagsResp response = await client.GetRLSPrincipalTagsAsync(
            new GetRLSPrincipalTagsReq { CollectionName = "book", PrincipalName = "alice" },
            TestContext.Current.CancellationToken);

        Assert.Empty(response.Tags);
    }

    [Fact]
    public async Task ListRLSPrincipals_returns_principal_names()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        server.Service.PrincipalNames.AddRange(new[] { "alice", "bob" });

        ListRLSPrincipalsResp response = await client.ListRLSPrincipalsAsync(
            new ListRLSPrincipalsReq { DatabaseName = "db1", CollectionName = "book" },
            TestContext.Current.CancellationToken);

        Assert.IsType<Grpc.ListRLSPrincipalsRequest>(server.Service.Requests["ListRLSPrincipals"]);
        Assert.Equal(new[] { "alice", "bob" }, response.PrincipalNames);
    }

    [Fact]
    public async Task DeleteRLSPrincipalTags_forwards_tag_keys()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DeleteRLSPrincipalTagsAsync(
            new DeleteRLSPrincipalTagsReq
            {
                DatabaseName = "db1",
                CollectionName = "book",
                PrincipalName = "alice",
                TagKeys = new[] { "level", "team" }
            },
            TestContext.Current.CancellationToken);

        Grpc.DeleteRLSPrincipalTagsRequest request = Assert.IsType<Grpc.DeleteRLSPrincipalTagsRequest>(server.Service.Requests["DeleteRLSPrincipalTags"]);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("book", request.CollectionName);
        Assert.Equal("alice", request.PrincipalName);
        Assert.Equal(new[] { "level", "team" }, request.TagKeys);
    }

    [Fact]
    public async Task DeleteRLSPrincipalTags_without_tag_keys_deletes_all()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DeleteRLSPrincipalTagsAsync(
            new DeleteRLSPrincipalTagsReq { DatabaseName = "db1", CollectionName = "book", PrincipalName = "alice" },
            TestContext.Current.CancellationToken);

        Grpc.DeleteRLSPrincipalTagsRequest request = Assert.IsType<Grpc.DeleteRLSPrincipalTagsRequest>(server.Service.Requests["DeleteRLSPrincipalTags"]);
        Assert.Empty(request.TagKeys);
    }

    [Fact]
    public async Task Search_forwards_rls_principal_and_skip_rls()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "book",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                RlsPrincipal = "alice",
                SkipRls = true
            },
            TestContext.Current.CancellationToken);

        Grpc.SearchRequest request = Assert.IsType<Grpc.SearchRequest>(server.Service.Requests["Search"]);
        Assert.Equal("alice", request.RlsPrincipal);
        Assert.True(request.SkipRls);
    }

    [Fact]
    public async Task HybridSearch_forwards_rls_principal_and_skip_rls()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "book",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }) },
                        MetricType = MetricType.L2,
                        Limit = 1
                    }
                ],
                Limit = 1,
                RlsPrincipal = "alice",
                SkipRls = true
            },
            TestContext.Current.CancellationToken);

        Grpc.HybridSearchRequest request = Assert.IsType<Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);
        Assert.Equal("alice", request.RlsPrincipal);
        Assert.True(request.SkipRls);
    }

    [Fact]
    public async Task QueryIterator_forwards_rls_principal_and_skip_rls()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildRlsSchema();

        await foreach (IReadOnlyList<FieldData> _ in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = new QueryParameters { Limit = 1 },
                RlsPrincipal = "alice",
                SkipRls = true
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
        }

        Grpc.QueryRequest request = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        Assert.Equal("alice", request.RlsPrincipal);
        Assert.True(request.SkipRls);
    }

    [Fact]
    public async Task SearchIterator_forwards_rls_principal_and_skip_rls()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildRlsSchema();
        server.Service.SearchIteratorToken = "tok-1";

        await foreach (SingleResult _ in client
            .SearchIteratorAsync(new SearchIteratorReq
            {
                CollectionName = "iter_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 2,
                BatchSize = 1,
                RlsPrincipal = "alice",
                SkipRls = true
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
        }

        Grpc.SearchRequest request = Assert.IsType<Grpc.SearchRequest>(server.Service.SearchRequests[0]);
        Assert.Equal("alice", request.RlsPrincipal);
        Assert.True(request.SkipRls);
    }

    private static CollectionSchema BuildRlsSchema()
    {
        var schema = new CollectionSchema { Name = "iter_coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateFloatVector("embedding", 4));
        return schema;
    }
}
