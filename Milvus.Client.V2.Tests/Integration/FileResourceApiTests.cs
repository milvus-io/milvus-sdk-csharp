using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class FileResourceApiTests
{
    [Fact]
    public async Task AddFileResource_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AddFileResourceAsync(
            new AddFileResourceReq { Name = "res-1", Path = "s3://bucket/path1" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.AddFileResourceRequest)server.Service.Requests["AddFileResource"];
        Assert.Equal("res-1", request.Name);
        Assert.Equal("s3://bucket/path1", request.Path);
    }

    [Fact]
    public async Task AddFileResource_rejects_empty_name()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.AddFileResourceAsync(
                new AddFileResourceReq { Path = "s3://bucket/path1" },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddFileResource_rejects_empty_path()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.AddFileResourceAsync(
                new AddFileResourceReq { Name = "res-1" },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveFileResource_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RemoveFileResourceAsync(
            new RemoveFileResourceReq { Name = "res-1" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.RemoveFileResourceRequest)server.Service.Requests["RemoveFileResource"];
        Assert.Equal("res-1", request.Name);
    }

    [Fact]
    public async Task RemoveFileResource_rejects_empty_name()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.RemoveFileResourceAsync(
                new RemoveFileResourceReq(),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListFileResources_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListFileResourcesResp response = await client.ListFileResourcesAsync(
            new ListFileResourcesReq(),
            TestContext.Current.CancellationToken);

        Assert.True(server.Service.Requests.ContainsKey("ListFileResources"));
        Assert.Equal(2, response.Resources.Count);
        Assert.Equal(1, response.Resources[0].Id);
        Assert.Equal("res-1", response.Resources[0].Name);
        Assert.Equal("s3://bucket/path1", response.Resources[0].Path);
        Assert.Equal(2, response.Resources[1].Id);
        Assert.Equal("res-2", response.Resources[1].Name);
        Assert.Equal("s3://bucket/path2", response.Resources[1].Path);
    }
}
