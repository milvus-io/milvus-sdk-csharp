using Xunit;

using Milvus.Client.V2.Requests.Utility;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class ExternalCollectionReqTests
{
    [Fact]
    public void RefreshExternalCollection_maps_fields_and_database()
    {
        var request = new RefreshExternalCollectionReq
        {
            DatabaseName = "db1",
            CollectionName = "coll",
            ExternalSource = "s3://bucket/path",
            ExternalSpec = "{\"k\":\"v\"}"
        };

        Grpc.RefreshExternalCollectionRequest grpc = request.ToGrpcRefreshExternalCollectionRequest("db1");

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("coll", grpc.CollectionName);
        Assert.Equal("s3://bucket/path", grpc.ExternalSource);
        Assert.Equal("{\"k\":\"v\"}", grpc.ExternalSpec);
    }

    [Fact]
    public void RefreshExternalCollection_defaults_optional_fields()
    {
        var request = new RefreshExternalCollectionReq { CollectionName = "coll" };

        Grpc.RefreshExternalCollectionRequest grpc = request.ToGrpcRefreshExternalCollectionRequest("default");

        Assert.Equal("default", grpc.DbName);
        Assert.Equal("coll", grpc.CollectionName);
        Assert.Equal("", grpc.ExternalSource);
        Assert.Equal("", grpc.ExternalSpec);
    }

    [Fact]
    public void RefreshExternalCollection_throws_when_collection_name_blank()
    {
        var request = new RefreshExternalCollectionReq { CollectionName = " " };

        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void GetRefreshExternalCollectionProgress_maps_job_id()
    {
        var request = new GetRefreshExternalCollectionProgressReq { JobId = 77 };

        Grpc.GetRefreshExternalCollectionProgressRequest grpc = request.ToGrpcGetRefreshExternalCollectionProgressRequest();

        Assert.Equal(77, grpc.JobId);
    }

    [Fact]
    public void GetRefreshExternalCollectionProgress_throws_when_job_id_not_positive()
    {
        var request = new GetRefreshExternalCollectionProgressReq { JobId = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => request.Validate());
    }

    [Fact]
    public void ListRefreshExternalCollectionJobs_maps_fields_and_database()
    {
        var request = new ListRefreshExternalCollectionJobsReq
        {
            DatabaseName = "db1",
            CollectionName = "coll"
        };

        Grpc.ListRefreshExternalCollectionJobsRequest grpc = request.ToGrpcListRefreshExternalCollectionJobsRequest("db1");

        Assert.Equal("db1", grpc.DbName);
        Assert.Equal("coll", grpc.CollectionName);
    }

    [Fact]
    public void ListRefreshExternalCollectionJobs_defaults_optional_fields()
    {
        var request = new ListRefreshExternalCollectionJobsReq();

        Grpc.ListRefreshExternalCollectionJobsRequest grpc = request.ToGrpcListRefreshExternalCollectionJobsRequest("default");

        Assert.Equal("default", grpc.DbName);
        Assert.Equal("", grpc.CollectionName);
    }
}
