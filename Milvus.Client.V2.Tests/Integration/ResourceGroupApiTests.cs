using Xunit;

using Milvus.Client.Grpc;
using Milvus.Client.V2;
using Milvus.Client.V2.Requests.ResourceGroup;
using Milvus.Client.V2.Responses.ResourceGroup;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class ResourceGroupApiTests
{
    [Fact]
    public async Task CreateResourceGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateResourceGroupAsync(
            new CreateResourceGroupReq { ResourceGroupName = "rg1" },
            TestContext.Current.CancellationToken);

        CreateResourceGroupRequest request = Assert.IsType<CreateResourceGroupRequest>(server.Service.Requests["CreateResourceGroup"]);
        Assert.Equal("rg1", request.ResourceGroup);
    }

    [Fact]
    public async Task DropResourceGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropResourceGroupAsync(
            new DropResourceGroupReq { ResourceGroupName = "rg1" },
            TestContext.Current.CancellationToken);

        DropResourceGroupRequest request = Assert.IsType<DropResourceGroupRequest>(server.Service.Requests["DropResourceGroup"]);
        Assert.Equal("rg1", request.ResourceGroup);
    }

    [Fact]
    public async Task UpdateResourceGroups_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UpdateResourceGroupsAsync(
            new UpdateResourceGroupsReq
            {
                ResourceGroups = new Dictionary<string, Milvus.Client.V2.Types.ResourceGroupConfig>
                {
                    ["rg1"] = new Milvus.Client.V2.Types.ResourceGroupConfig { RequestsNodeNum = 2 }
                }
            },
            TestContext.Current.CancellationToken);

        UpdateResourceGroupsRequest request = Assert.IsType<UpdateResourceGroupsRequest>(server.Service.Requests["UpdateResourceGroups"]);
        Assert.True(request.ResourceGroups.ContainsKey("rg1"));
        Assert.Equal(2, request.ResourceGroups["rg1"].Requests.NodeNum);
    }

    [Fact]
    public async Task TransferNode_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.TransferNodeAsync(
            new TransferNodeReq { SourceResourceGroup = "rg1", TargetResourceGroup = "rg2", NumNode = 2 },
            TestContext.Current.CancellationToken);

        TransferNodeRequest request = Assert.IsType<TransferNodeRequest>(server.Service.Requests["TransferNode"]);
        Assert.Equal("rg1", request.SourceResourceGroup);
        Assert.Equal("rg2", request.TargetResourceGroup);
        Assert.Equal(2, request.NumNode);
    }

    [Fact]
    public async Task TransferReplica_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.TransferReplicaAsync(
            new TransferReplicaReq
            {
                SourceResourceGroup = "rg1",
                TargetResourceGroup = "rg2",
                CollectionName = "coll",
                NumReplica = 2
            },
            TestContext.Current.CancellationToken);

        TransferReplicaRequest request = Assert.IsType<TransferReplicaRequest>(server.Service.Requests["TransferReplica"]);
        Assert.Equal("rg1", request.SourceResourceGroup);
        Assert.Equal("rg2", request.TargetResourceGroup);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(2L, request.NumReplica);
    }

    [Fact]
    public async Task ListResourceGroups_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListResourceGroupsResp response = await client.ListResourceGroupsAsync(
            new ListResourceGroupsReq(),
            TestContext.Current.CancellationToken);

        Assert.Empty(response.ResourceGroups);
        Assert.NotNull(server.Service.Requests["ListResourceGroups"]);
    }

    [Fact]
    public async Task DescribeResourceGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        DescribeResourceGroupResp response = await client.DescribeResourceGroupAsync(
            new DescribeResourceGroupReq { ResourceGroupName = "rg1" },
            TestContext.Current.CancellationToken);

        Assert.Equal("rg1", response.Name);
        DescribeResourceGroupRequest request =
            Assert.IsType<DescribeResourceGroupRequest>(server.Service.Requests["DescribeResourceGroup"]);
        Assert.Equal("rg1", request.ResourceGroup);
    }
}
