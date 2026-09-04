using Xunit;

using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Partition;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class IndexPartitionReqTests
{
    [Fact]
    public void CreateIndex_maps_fields_index_type_metric_type_and_extra_params()
    {
        var request = new CreateIndexReq { CollectionName = "book" };
        var index = new IndexParam("embedding", "my_idx", IndexType.Hnsw, SimilarityMetricType.Cosine);
        index.ExtraParams["nlist"] = 1024;
        index.ExtraParams["M"] = "32";

        Grpc.CreateIndexRequest grpc = request.ToGrpcCreateIndexRequest(index);

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("embedding", grpc.FieldName);
        Assert.Equal("my_idx", grpc.IndexName);
        Assert.Equal(4, grpc.ExtraParams.Count);
        Assert.Equal("HNSW", grpc.ExtraParams.Single(p => p.Key == "index_type").Value);
        Assert.Equal("COSINE", grpc.ExtraParams.Single(p => p.Key == "metric_type").Value);
        Assert.Equal("1024", grpc.ExtraParams.Single(p => p.Key == "nlist").Value);
        Assert.Equal("32", grpc.ExtraParams.Single(p => p.Key == "M").Value);
    }

    [Theory]
    [InlineData(-128)]
    [InlineData(0)]
    [InlineData("128")]
    [InlineData("-128")]
    [InlineData(" 128")]
    [InlineData("abc")]
    [InlineData(128.5)]
    public void CreateIndex_rejects_non_integral_or_non_positive_dimension_extra_param(object dimension)
    {
        var request = new CreateIndexReq { CollectionName = "book" };
        var index = new IndexParam("embedding", "my_idx", IndexType.Hnsw, SimilarityMetricType.Cosine);
        index.ExtraParams["dim"] = dimension;

        // The dimension must be a positive integral value (matching Java's isLegalDimensionValue): negative,
        // zero, strings, and floats are all rejected client-side.
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateIndexRequest(index));
    }

    [Fact]
    public void CreateIndex_accepts_positive_integral_dimension_extra_param()
    {
        var request = new CreateIndexReq { CollectionName = "book" };
        var index = new IndexParam("embedding", "my_idx", IndexType.Hnsw, SimilarityMetricType.Cosine);
        index.ExtraParams["dim"] = 128;

        Grpc.CreateIndexRequest grpc = request.ToGrpcCreateIndexRequest(index);

        Assert.Equal("128", grpc.ExtraParams.Single(p => p.Key == "dim").Value);
    }

    [Fact]
    public void CreateIndex_defaults_index_name_and_omits_optional_params()
    {
        var request = new CreateIndexReq { CollectionName = "book" };
        var index = new IndexParam("embedding");

        Grpc.CreateIndexRequest grpc = request.ToGrpcCreateIndexRequest(index);

        Assert.Equal("_default_idx", grpc.IndexName);
        Assert.Empty(grpc.ExtraParams);
    }

    [Fact]
    public void CreateIndex_throws_when_collection_blank()
    {
        var request = new CreateIndexReq { CollectionName = " " };
        var index = new IndexParam("embedding");
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateIndexRequest(index));
    }

    [Fact]
    public void CreateIndex_throws_when_field_blank()
    {
        // A blank field name is rejected by the IndexDesc constructor itself.
        Assert.Throws<ArgumentException>(() => new IndexParam(" "));
    }

    [Fact]
    public void DescribeIndex_maps_fields_and_index_name()
    {
        var request = new DescribeIndexReq
        {
            CollectionName = "book",
            FieldName = "embedding",
            IndexName = "my_idx",
            Timestamp = 123456
        };

        Grpc.DescribeIndexRequest grpc = request.ToGrpcDescribeIndexRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("embedding", grpc.FieldName);
        Assert.Equal("my_idx", grpc.IndexName);
        Assert.Equal(123456UL, grpc.Timestamp);
    }

    [Fact]
    public void DescribeIndex_defaults_index_name()
    {
        var request = new DescribeIndexReq { CollectionName = "book", FieldName = "embedding" };

        Grpc.DescribeIndexRequest grpc = request.ToGrpcDescribeIndexRequest();

        // Unset index name sends the empty name, which the proxy treats as "all indexes" (Java/PyMilvus parity).
        Assert.Equal("", grpc.IndexName);
    }

    [Fact]
    public void DescribeIndex_throws_when_collection_blank()
    {
        var request = new DescribeIndexReq { CollectionName = " ", FieldName = "embedding" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDescribeIndexRequest());
    }

    [Fact]
    public void DropIndex_maps_fields_and_defaults_index_name()
    {
        var request = new DropIndexReq { CollectionName = "book", FieldName = "embedding" };

        Grpc.DropIndexRequest grpc = request.ToGrpcDropIndexRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("embedding", grpc.FieldName);
        // Unset index name sends the empty name, which the proxy treats as "all indexes" (Java/PyMilvus parity).
        Assert.Equal("", grpc.IndexName);
    }

    [Fact]
    public void DropIndex_throws_when_collection_blank()
    {
        var request = new DropIndexReq { CollectionName = " ", FieldName = "embedding" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropIndexRequest());
    }

    [Fact]
    public void ListIndexes_maps_collection_name_only()
    {
        var request = new ListIndexesReq { CollectionName = "book" };

        Grpc.DescribeIndexRequest grpc = request.ToGrpcDescribeIndexRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("", grpc.FieldName);
        Assert.Equal("", grpc.IndexName);
    }

    [Fact]
    public void ListIndexes_maps_field_name_filter()
    {
        var request = new ListIndexesReq { CollectionName = "book", FieldName = "embedding" };

        Grpc.DescribeIndexRequest grpc = request.ToGrpcDescribeIndexRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("embedding", grpc.FieldName);
    }

    [Fact]
    public void ListIndexes_throws_when_collection_blank()
    {
        var request = new ListIndexesReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDescribeIndexRequest());
    }

    [Fact]
    public void CreatePartition_maps_names()
    {
        var request = new CreatePartitionReq { CollectionName = "book", PartitionName = "p1" };

        Grpc.CreatePartitionRequest grpc = request.ToGrpcCreatePartitionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
    }

    [Fact]
    public void CreatePartition_throws_when_collection_blank()
    {
        var request = new CreatePartitionReq { CollectionName = " ", PartitionName = "p1" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreatePartitionRequest());
    }

    [Fact]
    public void CreatePartition_throws_when_partition_blank()
    {
        var request = new CreatePartitionReq { CollectionName = "book", PartitionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreatePartitionRequest());
    }

    [Fact]
    public void DropPartition_maps_names()
    {
        var request = new DropPartitionReq { CollectionName = "book", PartitionName = "p1" };

        Grpc.DropPartitionRequest grpc = request.ToGrpcDropPartitionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
    }

    [Fact]
    public void DropPartition_throws_when_collection_blank()
    {
        var request = new DropPartitionReq { CollectionName = " ", PartitionName = "p1" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropPartitionRequest());
    }

    [Fact]
    public void GetPartitionStats_maps_names()
    {
        var request = new GetPartitionStatsReq { CollectionName = "book", PartitionName = "p1" };

        Grpc.GetPartitionStatisticsRequest grpc = request.ToGrpcGetPartitionStatisticsRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
    }

    [Fact]
    public void GetPartitionStats_throws_when_collection_blank()
    {
        var request = new GetPartitionStatsReq { CollectionName = " ", PartitionName = "p1" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcGetPartitionStatisticsRequest());
    }

    [Fact]
    public void HasPartition_maps_names()
    {
        var request = new HasPartitionReq { CollectionName = "book", PartitionName = "p1" };

        Grpc.HasPartitionRequest grpc = request.ToGrpcHasPartitionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("p1", grpc.PartitionName);
    }

    [Fact]
    public void HasPartition_throws_when_partition_blank()
    {
        var request = new HasPartitionReq { CollectionName = "book", PartitionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcHasPartitionRequest());
    }

    [Fact]
    public void ListPartitions_maps_collection_name()
    {
        var request = new ListPartitionsReq { CollectionName = "book" };

        Grpc.ShowPartitionsRequest grpc = request.ToGrpcShowPartitionsRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void ListPartitions_throws_when_collection_blank()
    {
        var request = new ListPartitionsReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcShowPartitionsRequest());
    }

    [Fact]
    public void LoadPartitions_maps_names_and_replica_number()
    {
        var request = new LoadPartitionsReq
        {
            CollectionName = "book",
            PartitionNames = new[] { "p1", "p2" },
            ReplicaNumber = 2,
            Refresh = true,
            LoadFields = new[] { "field1" },
            SkipLoadDynamicField = true,
            TargetResourceGroups = new[] { "rg1" }
        };

        Grpc.LoadPartitionsRequest grpc = request.ToGrpcLoadPartitionsRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(2, grpc.ReplicaNumber);
        Assert.Equal(new[] { "p1", "p2" }, grpc.PartitionNames);
        Assert.True(grpc.Refresh);
        Assert.Equal(new[] { "field1" }, grpc.LoadFields);
        Assert.True(grpc.SkipLoadDynamicField);
        Assert.Equal(new[] { "rg1" }, grpc.ResourceGroups);
    }

    [Fact]
    public void LoadPartitions_defaults_replica_number_to_one()
    {
        var request = new LoadPartitionsReq { CollectionName = "book", PartitionNames = new[] { "p1" } };

        Grpc.LoadPartitionsRequest grpc = request.ToGrpcLoadPartitionsRequest();

        Assert.Equal(1, grpc.ReplicaNumber);
        Assert.Equal(new[] { "p1" }, grpc.PartitionNames);
    }

    [Fact]
    public void LoadPartitions_throws_when_partition_names_empty()
    {
        var request = new LoadPartitionsReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcLoadPartitionsRequest());
    }

    [Fact]
    public void LoadPartitions_maps_priority_to_load_params()
    {
        var request = new LoadPartitionsReq
        {
            CollectionName = "book",
            PartitionNames = new[] { "p1" },
            Priority = "High"
        };

        Grpc.LoadPartitionsRequest grpc = request.ToGrpcLoadPartitionsRequest();

        Assert.Equal("high", grpc.LoadParams["load_priority"]);
    }

    [Fact]
    public void ReleasePartitions_maps_names()
    {
        var request = new ReleasePartitionsReq { CollectionName = "book", PartitionNames = new[] { "p1", "p2" } };

        Grpc.ReleasePartitionsRequest grpc = request.ToGrpcReleasePartitionsRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(new[] { "p1", "p2" }, grpc.PartitionNames);
    }

    [Fact]
    public void ReleasePartitions_throws_when_partition_names_empty()
    {
        var request = new ReleasePartitionsReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcReleasePartitionsRequest());
    }
}
