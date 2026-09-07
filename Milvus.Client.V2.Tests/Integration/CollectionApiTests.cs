using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class CollectionApiTests
{
    [Fact]
    public async Task CreateCollection_forwards_request_and_succeeds()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = "coll",
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            }
        }, TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastCreatedCollectionName);
    }

    [Fact]
    public async Task HasCollection_maps_response()
    {
        using var server = new MockMilvusServer { Service = { HasCollectionResult = true } };
        using MilvusClientV2 client = server.CreateClient();

        HasCollectionResp response =
            await client.HasCollectionAsync(new HasCollectionReq { CollectionName = "coll" }, TestContext.Current.CancellationToken);

        Assert.True(response.Has);
        Assert.Equal("coll", server.Service.LastCheckedCollectionName);
    }

    [Fact]
    public async Task ListCollections_maps_response()
    {
        using var server = new MockMilvusServer();
        server.Service.CollectionNames.Add("a");
        server.Service.CollectionNames.Add("b");
        using MilvusClientV2 client = server.CreateClient();

        ListCollectionsResp response = await client.ListCollectionsAsync(new ListCollectionsReq(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "a", "b" }, response.CollectionNames);
    }

    [Fact]
    public async Task Server_error_maps_to_MilvusException()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // Connect successfully first (lazy), then make the operation itself fail.
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        server.Service.FailureStatus = new Milvus.Client.Grpc.Status
        {
            Code = (int)MilvusErrorCode.CollectionNotFound,
            Reason = "collection not found"
        };

        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(() =>
            client.HasCollectionAsync(new HasCollectionReq { CollectionName = "missing" }, TestContext.Current.CancellationToken));

        Assert.Equal(MilvusErrorCode.CollectionNotFound, exception.ErrorCode);
        Assert.Contains("collection not found", exception.Message);
    }
}
