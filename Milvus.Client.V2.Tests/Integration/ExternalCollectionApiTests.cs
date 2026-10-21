using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class ExternalCollectionApiTests
{
    [Fact]
    public async Task RefreshExternalCollection_forwards_request_and_maps_job_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        RefreshExternalCollectionResp response = await client.RefreshExternalCollectionAsync(
            new RefreshExternalCollectionReq
            {
                DatabaseName = "db1",
                CollectionName = "coll",
                ExternalSource = "s3://bucket/path1",
                ExternalSpec = "{\"k\":\"v\"}"
            },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.RefreshExternalCollectionRequest)server.Service.Requests["RefreshExternalCollection"];
        Assert.Equal("db1", request.DbName);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal("s3://bucket/path1", request.ExternalSource);
        Assert.Equal("{\"k\":\"v\"}", request.ExternalSpec);

        Assert.Equal(42, response.JobId);
    }

    [Fact]
    public async Task RefreshExternalCollection_rejects_empty_collection_name()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.RefreshExternalCollectionAsync(
                new RefreshExternalCollectionReq(),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefreshExternalCollection_defaults_database_to_current_when_omitted()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RefreshExternalCollectionAsync(
            new RefreshExternalCollectionReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.RefreshExternalCollectionRequest)server.Service.Requests["RefreshExternalCollection"];
        Assert.Equal("default", request.DbName);
    }

    [Fact]
    public async Task ListRefreshExternalCollectionJobs_uses_switched_database_when_omitted()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UseDatabaseAsync(
            new UseDatabaseReq { DatabaseName = "db1" },
            TestContext.Current.CancellationToken);

        await client.ListRefreshExternalCollectionJobsAsync(
            new ListRefreshExternalCollectionJobsReq { CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ListRefreshExternalCollectionJobsRequest)server.Service.Requests["ListRefreshExternalCollectionJobs"];
        Assert.Equal("db1", request.DbName);
    }

    [Fact]
    public async Task GetRefreshExternalCollectionProgress_forwards_request_and_maps_job_info()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        GetRefreshExternalCollectionProgressResp response = await client.GetRefreshExternalCollectionProgressAsync(
            new GetRefreshExternalCollectionProgressReq { JobId = 42 },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.GetRefreshExternalCollectionProgressRequest)server.Service.Requests["GetRefreshExternalCollectionProgress"];
        Assert.Equal(42, request.JobId);

        Assert.Equal(42, response.JobInfo.JobId);
        Assert.Equal("coll", response.JobInfo.CollectionName);
        Assert.Equal(RefreshExternalCollectionState.Completed, response.JobInfo.State);
        Assert.Equal(100, response.JobInfo.Progress);
        Assert.Equal("s3://bucket/path1", response.JobInfo.ExternalSource);
    }

    [Fact]
    public async Task GetRefreshExternalCollectionProgress_rejects_non_positive_job_id()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.GetRefreshExternalCollectionProgressAsync(
                new GetRefreshExternalCollectionProgressReq { JobId = 0 },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListRefreshExternalCollectionJobs_forwards_request_and_maps_jobs()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListRefreshExternalCollectionJobsResp response = await client.ListRefreshExternalCollectionJobsAsync(
            new ListRefreshExternalCollectionJobsReq { DatabaseName = "db1", CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        var request = (Milvus.Client.Grpc.ListRefreshExternalCollectionJobsRequest)server.Service.Requests["ListRefreshExternalCollectionJobs"];
        Assert.Equal("db1", request.DbName);
        Assert.Equal("coll", request.CollectionName);

        Assert.Equal(2, response.Jobs.Count);
        Assert.Equal(1, response.Jobs[0].JobId);
        Assert.Equal("coll-1", response.Jobs[0].CollectionName);
        Assert.Equal(RefreshExternalCollectionState.Pending, response.Jobs[0].State);
        Assert.Equal(2, response.Jobs[1].JobId);
        Assert.Equal("coll-2", response.Jobs[1].CollectionName);
        Assert.Equal(RefreshExternalCollectionState.Failed, response.Jobs[1].State);
        Assert.Equal("boom", response.Jobs[1].Reason);
    }
}
