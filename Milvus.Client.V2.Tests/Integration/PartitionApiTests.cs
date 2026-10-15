using Xunit;

using Milvus.Client.Grpc;
using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Partition;
using Milvus.Client.V2.Responses.Partition;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class PartitionApiTests
{
    [Fact]
    public async Task CreatePartition_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreatePartitionAsync(
            new CreatePartitionReq { CollectionName = "coll", PartitionName = "p1" },
            TestContext.Current.CancellationToken);

        CreatePartitionRequest request = Assert.IsType<CreatePartitionRequest>(server.Service.Requests["CreatePartition"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("p1", request.PartitionName);
    }

    [Fact]
    public async Task DropPartition_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropPartitionAsync(
            new DropPartitionReq { CollectionName = "coll", PartitionName = "p1" },
            TestContext.Current.CancellationToken);

        DropPartitionRequest request = Assert.IsType<DropPartitionRequest>(server.Service.Requests["DropPartition"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("p1", request.PartitionName);
    }

    [Fact]
    public async Task HasPartition_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        HasPartitionResp response = await client.HasPartitionAsync(
            new HasPartitionReq { CollectionName = "coll", PartitionName = "p1" },
            TestContext.Current.CancellationToken);

        Assert.True(response.Has);
        HasPartitionRequest request = Assert.IsType<HasPartitionRequest>(server.Service.Requests["HasPartition"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("p1", request.PartitionName);
    }

    [Fact]
    public async Task ListPartitions_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListPartitionsResp response = await client.ListPartitionsAsync(
            new ListPartitionsReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        Assert.Empty(response.PartitionNames);
        ShowPartitionsRequest request = Assert.IsType<ShowPartitionsRequest>(server.Service.Requests["ShowPartitions"]);
        Assert.Equal("coll", request.CollectionName);
    }

    [Fact]
    public async Task LoadPartitions_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.LoadPartitionsAsync(
            new LoadPartitionsReq
            {
                CollectionName = "coll",
                PartitionNames = new[] { "p1", "p2" },
                ReplicaNumber = 2
            },
            TestContext.Current.CancellationToken);

        LoadPartitionsRequest request = Assert.IsType<LoadPartitionsRequest>(server.Service.Requests["LoadPartitions"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(new[] { "p1", "p2" }, request.PartitionNames);
        Assert.Equal(2, request.ReplicaNumber);
    }

    [Fact]
    public async Task ReleasePartitions_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ReleasePartitionsAsync(
            new ReleasePartitionsReq { CollectionName = "coll", PartitionNames = new[] { "p1", "p2" } },
            TestContext.Current.CancellationToken);

        ReleasePartitionsRequest request = Assert.IsType<ReleasePartitionsRequest>(server.Service.Requests["ReleasePartitions"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(new[] { "p1", "p2" }, request.PartitionNames);
    }

    [Fact]
    public async Task GetPartitionStats_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetPartitionStatsResp response = await client.GetPartitionStatsAsync(
            new GetPartitionStatsReq { CollectionName = "coll", PartitionName = "p1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(0, response.RowCount);
        GetPartitionStatisticsRequest request =
            Assert.IsType<GetPartitionStatisticsRequest>(server.Service.Requests["GetPartitionStatistics"]);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("p1", request.PartitionName);
    }

}
