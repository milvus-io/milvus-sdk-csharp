using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Aliases;
using Milvus.Client.V2.Responses.Aliases;
using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class AliasApiTests
{
    // ---- Alias ----

    [Fact]
    public async Task CreateAlias_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateAliasAsync(
            new CreateAliasReq { CollectionName = "coll", Alias = "my_alias" },
            TestContext.Current.CancellationToken);

        CreateAliasRequest request = Assert.IsType<CreateAliasRequest>(server.Service.Requests["CreateAlias"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("my_alias", request.Alias);
    }

    [Fact]
    public async Task DropAlias_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropAliasAsync(
            new DropAliasReq { Alias = "my_alias" },
            TestContext.Current.CancellationToken);

        DropAliasRequest request = Assert.IsType<DropAliasRequest>(server.Service.Requests["DropAlias"]);
        Assert.Equal("my_alias", request.Alias);
    }

    [Fact]
    public async Task AlterAlias_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterAliasAsync(
            new AlterAliasReq { CollectionName = "other_coll", Alias = "my_alias" },
            TestContext.Current.CancellationToken);

        AlterAliasRequest request = Assert.IsType<AlterAliasRequest>(server.Service.Requests["AlterAlias"]);
        Assert.Equal("other_coll", request.CollectionName);
        Assert.Equal("my_alias", request.Alias);
    }

    [Fact]
    public async Task DescribeAlias_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        DescribeAliasResp response = await client.DescribeAliasAsync(
            new DescribeAliasReq { Alias = "my_alias" },
            TestContext.Current.CancellationToken);

        DescribeAliasRequest request = Assert.IsType<DescribeAliasRequest>(server.Service.Requests["DescribeAlias"]);
        Assert.Equal("my_alias", request.Alias);
        Assert.Equal("my_alias", response.Alias);
    }

    [Fact]
    public async Task ListAliases_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListAliasesAsync(
            new ListAliasesReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        ListAliasesRequest request = Assert.IsType<ListAliasesRequest>(server.Service.Requests["ListAliases"]);
        Assert.Equal("coll", request.CollectionName);
    }
}
