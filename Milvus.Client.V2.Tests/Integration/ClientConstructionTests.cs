using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class ClientConstructionTests
{
    [Fact]
    public async Task ConnectAsync_sends_connect_rpc_with_client_info()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        Assert.Null(server.Service.LastConnectClientInfo);   // not connected until ConnectAsync/first call

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(server.Service.LastConnectClientInfo);
        Assert.Equal("CSharp", server.Service.LastConnectClientInfo!.SdkType);
        Assert.False(string.IsNullOrEmpty(server.Service.LastConnectClientInfo!.SdkVersion));
    }

    [Fact]
    public async Task ConnectAsync_throws_when_connect_rpc_fails()
    {
        using var server = new MockMilvusServer
        {
            Service =
            {
                FailureStatus = new Milvus.Client.Grpc.Status
                {
                    Code = (int)MilvusErrorCode.ForceDeny,
                    Reason = "denied"
                }
            }
        };
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<MilvusException>(() =>
            client.ConnectAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task First_api_call_connects_lazily()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // No explicit ConnectAsync; the first operation should connect lazily.
        await client.CreateCollectionAsync(
            new CreateCollectionReq
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
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(server.Service.LastConnectClientInfo);
        Assert.Equal("coll", server.Service.LastCreatedCollectionName);
    }
}
