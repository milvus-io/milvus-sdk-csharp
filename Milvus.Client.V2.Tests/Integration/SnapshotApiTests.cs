using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Snapshot;
using Milvus.Client.V2.Responses.Snapshot;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class SnapshotApiTests
{
    [Fact]
    public async Task CreateSnapshot_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UseDatabaseAsync(
            new Requests.Utility.UseDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        await client.CreateSnapshotAsync(
            new CreateSnapshotReq
            {
                SnapshotName = "snap-1",
                CollectionName = "coll",
                DatabaseName = "db1",
                Description = "desc",
                CompactionProtectionSeconds = 3600
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.CreateSnapshotRequest)server.Service.Requests["CreateSnapshot"];
        Assert.Equal("snap-1", request.Name);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("desc", request.Description);
        Assert.Equal(3600, request.CompactionProtectionSeconds);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("db1", server.Service.LastRequestDbName);
    }

    [Fact]
    public async Task CreateSnapshot_rejects_empty_name()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.CreateSnapshotAsync(
                new CreateSnapshotReq { CollectionName = "coll" },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSnapshot_rejects_negative_compaction_protection()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.CreateSnapshotAsync(
                new CreateSnapshotReq
                {
                    SnapshotName = "snap-1",
                    CollectionName = "coll",
                    CompactionProtectionSeconds = -1
                },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DropSnapshot_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropSnapshotAsync(
            new DropSnapshotReq { SnapshotName = "snap-1", CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.DropSnapshotRequest)server.Service.Requests["DropSnapshot"];
        Assert.Equal("snap-1", request.Name);
        Assert.Equal("coll", request.CollectionName);
    }

    [Fact]
    public async Task ListSnapshots_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListSnapshotsResp response = await client.ListSnapshotsAsync(
            new ListSnapshotsReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ListSnapshotsRequest)server.Service.Requests["ListSnapshots"];
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(new[] { "snapshot-1", "snapshot-2" }, response.Snapshots);
    }

    [Fact]
    public async Task DescribeSnapshot_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        DescribeSnapshotResp response = await client.DescribeSnapshotAsync(
            new DescribeSnapshotReq { SnapshotName = "snap-1", CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.DescribeSnapshotRequest)server.Service.Requests["DescribeSnapshot"];
        Assert.Equal("snap-1", request.Name);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("snap-1", response.Name);
        Assert.Equal("coll", response.CollectionName);
        Assert.Equal("mock description", response.Description);
        Assert.Equal(new[] { "p0", "p1" }, response.PartitionNames);
        Assert.Equal(1234567890, response.CreateTs);
        Assert.Equal("s3://bucket/path", response.S3Location);
    }

    [Fact]
    public async Task RestoreSnapshot_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        RestoreSnapshotResp response = await client.RestoreSnapshotAsync(
            new RestoreSnapshotReq
            {
                SnapshotName = "snap-1",
                SourceCollectionName = "src",
                TargetCollectionName = "dst",
                SourceDbName = "db1",
                TargetDbName = "db2"
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.RestoreSnapshotRequest)server.Service.Requests["RestoreSnapshot"];
        Assert.Equal("snap-1", request.Name);
        Assert.Equal("src", request.CollectionName);
        Assert.Equal("dst", request.TargetCollectionName);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("db2", request.TargetDbName);
        Assert.Equal(77, response.JobId);
    }

    [Fact]
    public async Task GetRestoreSnapshotState_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetRestoreSnapshotStateResp response = await client.GetRestoreSnapshotStateAsync(
            new GetRestoreSnapshotStateReq { JobId = 77 },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetRestoreSnapshotStateRequest)server.Service.Requests["GetRestoreSnapshotState"];
        Assert.Equal(77, request.JobId);
        Assert.Equal(77, response.JobInfo.JobId);
        Assert.Equal("snapshot-1", response.JobInfo.SnapshotName);
        Assert.Equal("coll", response.JobInfo.CollectionName);
        Assert.Equal(RestoreSnapshotState.Completed, response.JobInfo.State);
        Assert.Equal(100, response.JobInfo.Progress);
        Assert.Equal(500UL, response.JobInfo.TimeCost);
    }

    [Fact]
    public async Task GetRestoreSnapshotState_rejects_non_positive_job_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.GetRestoreSnapshotStateAsync(
                new GetRestoreSnapshotStateReq { JobId = 0 },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListRestoreSnapshotJobs_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListRestoreSnapshotJobsResp response = await client.ListRestoreSnapshotJobsAsync(
            new ListRestoreSnapshotJobsReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ListRestoreSnapshotJobsRequest)server.Service.Requests["ListRestoreSnapshotJobs"];
        Assert.Equal("coll", request.CollectionName);
        RestoreSnapshotJobInfo job = Assert.Single(response.Jobs);
        Assert.Equal(1, job.JobId);
        Assert.Equal("snapshot-1", job.SnapshotName);
        Assert.Equal(RestoreSnapshotState.Pending, job.State);
    }

    [Fact]
    public async Task PinSnapshotData_forwards_request_and_maps_response()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UseDatabaseAsync(
            new Requests.Utility.UseDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        PinSnapshotDataResp response = await client.PinSnapshotDataAsync(
            new PinSnapshotDataReq
            {
                SnapshotName = "snap-1",
                CollectionName = "coll",
                DatabaseName = "db1",
                TtlSeconds = 3600
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.PinSnapshotDataRequest)server.Service.Requests["PinSnapshotData"];
        Assert.Equal("snap-1", request.Name);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(3600, request.TtlSeconds);
        Assert.Equal("db1", request.DbName);
        Assert.Equal("db1", server.Service.LastRequestDbName);
        Assert.Equal(99, response.PinId);
    }

    [Fact]
    public async Task PinSnapshotData_rejects_negative_ttl()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.PinSnapshotDataAsync(
                new PinSnapshotDataReq
                {
                    SnapshotName = "snap-1",
                    CollectionName = "coll",
                    TtlSeconds = -1
                },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnpinSnapshotData_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UnpinSnapshotDataAsync(
            new UnpinSnapshotDataReq { PinId = 99 },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.UnpinSnapshotDataRequest)server.Service.Requests["UnpinSnapshotData"];
        Assert.Equal(99, request.PinId);
    }

    [Fact]
    public async Task UnpinSnapshotData_rejects_non_positive_pin_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.UnpinSnapshotDataAsync(
                new UnpinSnapshotDataReq { PinId = 0 },
                TestContext.Current.CancellationToken));
    }
}
