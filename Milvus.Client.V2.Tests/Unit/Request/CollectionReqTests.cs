using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class CollectionReqTests
{
    [Fact]
    public void AddCollectionField_maps_clustering_key()
    {
        var field = new FieldSchema("tenant_id", DataType.Int64) { IsClusteringKey = true, Nullable = true };

        var request = new AddCollectionFieldReq { CollectionName = "book", Field = field };

        Grpc.AddCollectionFieldRequest grpc = request.ToGrpcAddCollectionFieldRequest();
        Grpc.FieldSchema grpcField = Grpc.FieldSchema.Parser.ParseFrom(grpc.Schema);

        Assert.True(grpcField.IsClusteringKey);
    }

    [Fact]
    public void AddCollectionField_rejects_non_nullable_field()
    {
        var field = new FieldSchema("tenant_id", DataType.Int64) { IsClusteringKey = true };

        var request = new AddCollectionFieldReq { CollectionName = "book", Field = field };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcAddCollectionFieldRequest());
        Assert.Contains("must be nullable", ex.Message);
    }

    [Fact]
    public void CreateCollection_maps_clustering_key()
    {
        var field = new FieldSchema("tenant_id", DataType.Int64, isPrimaryKey: true) { IsClusteringKey = true };

        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema { Fields = { field } }
        };

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        Grpc.CollectionSchema grpcSchema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);

        Assert.True(grpcSchema.Fields.Single().IsClusteringKey);
    }

    [Fact]
    public void CreateCollection_rejects_binary_vector_dimension_not_multiple_of_8()
    {
        var binaryField = new FieldSchema("bvec", DataType.BinaryVector) { Dimension = 12 };
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema { Fields = { binaryField } }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => request.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void AddCollectionField_maps_varchar_field_with_params()
    {
        var field = new FieldSchema("title", DataType.VarChar, isPrimaryKey: true, autoId: true, isPartitionKey: true, description: "book title")
        {
            MaxLength = 100,
            Nullable = true,
            DefaultValue = "N/A",
            EnableAnalyzer = true,
            AnalyzerParams = new Dictionary<string, object> { ["type"] = "english" }
        };

        var request = new AddCollectionFieldReq { CollectionName = "book", Field = field };

        Grpc.AddCollectionFieldRequest grpc = request.ToGrpcAddCollectionFieldRequest();
        Grpc.FieldSchema grpcField = Grpc.FieldSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("title", grpcField.Name);
        Assert.Equal(Grpc.DataType.VarChar, grpcField.DataType);
        Assert.True(grpcField.IsPrimaryKey);
        Assert.True(grpcField.AutoID);
        Assert.True(grpcField.IsPartitionKey);
        Assert.Equal("book title", grpcField.Description);
        Assert.True(grpcField.Nullable);
        Assert.Equal("N/A", grpcField.DefaultValue.StringData);
        Assert.Equal("100", grpcField.TypeParams.Single(p => p.Key == "max_length").Value);
        Assert.Equal("true", grpcField.TypeParams.Single(p => p.Key == "enable_analyzer").Value);
        Assert.Equal("{\"type\":\"english\"}", grpcField.TypeParams.Single(p => p.Key == "analyzer_params").Value);
    }

    [Fact]
    public void AddCollectionField_maps_enable_match_and_multi_analyzer_params()
    {
        var field = new FieldSchema("text", DataType.VarChar)
        {
            MaxLength = 256,
            Nullable = true,
            EnableMatch = true,
            MultiAnalyzerParams = new Dictionary<string, object>
            {
                ["languages"] = new[] { "en", "zh" },
                ["weights"] = 1.0
            }
        };

        var request = new AddCollectionFieldReq { CollectionName = "book", Field = field };

        Grpc.AddCollectionFieldRequest grpc = request.ToGrpcAddCollectionFieldRequest();
        Grpc.FieldSchema grpcField = Grpc.FieldSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Equal("true", grpcField.TypeParams.Single(p => p.Key == "enable_match").Value);
        Assert.Equal("{\"languages\":[\"en\",\"zh\"],\"weights\":1}",
            grpcField.TypeParams.Single(p => p.Key == "multi_analyzer_params").Value);
    }

    [Fact]
    public void CreateCollection_maps_enable_match_and_multi_analyzer_params()
    {
        var field = new FieldSchema("text", DataType.VarChar)
        {
            MaxLength = 256,
            EnableMatch = true,
            MultiAnalyzerParams = new Dictionary<string, object> { ["languages"] = new[] { "en" } }
        };

        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema { Fields = { field } }
        };

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        Grpc.CollectionSchema grpcSchema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);
        Grpc.FieldSchema grpcField = grpcSchema.Fields.Single();

        Assert.Equal("true", grpcField.TypeParams.Single(p => p.Key == "enable_match").Value);
        Assert.Equal("{\"languages\":[\"en\"]}",
            grpcField.TypeParams.Single(p => p.Key == "multi_analyzer_params").Value);
    }

    [Fact]
    public void AddCollectionField_maps_array_field_element_type()
    {
        var field = new FieldSchema("tags", DataType.Array, description: "tags")
        {
            ElementDataType = DataType.Int64,
            MaxCapacity = 8,
            Nullable = true
        };

        var request = new AddCollectionFieldReq { CollectionName = "book", Field = field };

        Grpc.AddCollectionFieldRequest grpc = request.ToGrpcAddCollectionFieldRequest();
        Grpc.FieldSchema grpcField = Grpc.FieldSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Equal(Grpc.DataType.Array, grpcField.DataType);
        Assert.Equal(Grpc.DataType.Int64, grpcField.ElementType);
        Assert.Equal("8", grpcField.TypeParams.Single(p => p.Key == "max_capacity").Value);
    }

    [Fact]
    public void AddCollectionField_maps_vector_dimension()
    {
        FieldSchema embedding = FieldSchema.CreateFloatVector("embedding", 128, "semantic embedding");
        embedding.Nullable = true;

        var request = new AddCollectionFieldReq
        {
            CollectionName = "book",
            Field = embedding
        };

        Grpc.AddCollectionFieldRequest grpc = request.ToGrpcAddCollectionFieldRequest();
        Grpc.FieldSchema grpcField = Grpc.FieldSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Equal(Grpc.DataType.FloatVector, grpcField.DataType);
        Assert.Equal("128", grpcField.TypeParams.Single(p => p.Key == "dim").Value);
    }

    [Fact]
    public void AddCollectionField_throws_when_collection_blank()
    {
        var request = new AddCollectionFieldReq { CollectionName = " ", Field = new FieldSchema("f", DataType.Int64) };
        Assert.Throws<ArgumentException>(() => request.ToGrpcAddCollectionFieldRequest());
    }

    [Fact]
    public void AddCollectionField_throws_when_field_null()
    {
        var request = new AddCollectionFieldReq { CollectionName = "book", Field = null! };
        Assert.Throws<ArgumentNullException>(() => request.ToGrpcAddCollectionFieldRequest());
    }

    [Fact]
    public void AddCollectionFunction_maps_function_and_collection_id()
    {
        var request = new AddCollectionFunctionReq
        {
            CollectionName = "book",
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse", "bm25 scoring")
        };

        Grpc.AddCollectionFunctionRequest grpc = request.ToGrpcAddCollectionFunctionRequest(42);

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(42L, grpc.CollectionID);
        Assert.Equal("bm25_fn", grpc.FunctionSchema.Name);
        Assert.Equal(Grpc.FunctionType.Bm25, grpc.FunctionSchema.Type);
        Assert.Equal("bm25 scoring", grpc.FunctionSchema.Description);
        Assert.Equal(new[] { "text" }, grpc.FunctionSchema.InputFieldNames);
        Assert.Equal(new[] { "sparse" }, grpc.FunctionSchema.OutputFieldNames);
    }

    [Fact]
    public void AddCollectionFunction_throws_when_collection_blank()
    {
        var request = new AddCollectionFunctionReq
        {
            CollectionName = " ",
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse")
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcAddCollectionFunctionRequest(42));
    }

    [Fact]
    public void AlterCollectionFunction_maps_function_name_and_schema()
    {
        var request = new AlterCollectionFunctionReq
        {
            CollectionName = "book",
            FunctionName = "bm25_fn",
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse")
        };

        Grpc.AlterCollectionFunctionRequest grpc = request.ToGrpcAlterCollectionFunctionRequest(42);

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(42L, grpc.CollectionID);
        Assert.Equal("bm25_fn", grpc.FunctionName);
        Assert.Equal("bm25_fn", grpc.FunctionSchema.Name);
        Assert.Equal(Grpc.FunctionType.Bm25, grpc.FunctionSchema.Type);
        Assert.Equal(new[] { "text" }, grpc.FunctionSchema.InputFieldNames);
        Assert.Equal(new[] { "sparse" }, grpc.FunctionSchema.OutputFieldNames);
    }

    [Fact]
    public void AlterCollectionFunction_throws_when_function_name_blank()
    {
        var request = new AlterCollectionFunctionReq
        {
            CollectionName = "book",
            FunctionName = " ",
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse")
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionFunctionRequest(42));
    }

    [Fact]
    public void DescribeCollection_maps_collection_name()
    {
        var request = new DescribeCollectionReq { CollectionName = "book" };

        Grpc.DescribeCollectionRequest grpc = request.ToGrpcDescribeCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void DescribeCollection_throws_when_name_blank()
    {
        var request = new DescribeCollectionReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDescribeCollectionRequest());
    }

    [Fact]
    public void DropCollectionFunction_maps_function_name_and_collection_id()
    {
        var request = new DropCollectionFunctionReq { CollectionName = "book", FunctionName = "bm25_fn" };

        Grpc.DropCollectionFunctionRequest grpc = request.ToGrpcDropCollectionFunctionRequest(42);

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(42L, grpc.CollectionID);
        Assert.Equal("bm25_fn", grpc.FunctionName);
    }

    [Fact]
    public void DropCollectionFunction_throws_when_function_name_blank()
    {
        var request = new DropCollectionFunctionReq { CollectionName = "book", FunctionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropCollectionFunctionRequest(42));
    }

    [Fact]
    public void DropCollection_maps_collection_name()
    {
        var request = new DropCollectionReq { CollectionName = "book" };

        Grpc.DropCollectionRequest grpc = request.ToGrpcDropCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void DropCollection_throws_when_name_blank()
    {
        var request = new DropCollectionReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropCollectionRequest());
    }

    [Fact]
    public void GetCollectionStats_maps_collection_name()
    {
        var request = new GetCollectionStatsReq { CollectionName = "book" };

        Grpc.GetCollectionStatisticsRequest grpc = request.ToGrpcGetCollectionStatisticsRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void GetCollectionStats_throws_when_name_blank()
    {
        var request = new GetCollectionStatsReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcGetCollectionStatisticsRequest());
    }

    [Fact]
    public void GetLoadState_maps_collection_name()
    {
        var request = new GetLoadStateReq { CollectionName = "book" };

        Grpc.GetLoadStateRequest grpc = request.ToGrpcGetLoadStateRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void GetLoadState_throws_when_name_blank()
    {
        var request = new GetLoadStateReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcGetLoadStateRequest());
    }

    [Fact]
    public void HasCollection_maps_collection_name()
    {
        var request = new HasCollectionReq { CollectionName = "book" };

        Grpc.HasCollectionRequest grpc = request.ToGrpcHasCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void HasCollection_throws_when_name_blank()
    {
        var request = new HasCollectionReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcHasCollectionRequest());
    }

    [Fact]
    public void ListCollections_builds_empty_request()
    {
        var request = new ListCollectionsReq();

        Grpc.ShowCollectionsRequest grpc = request.ToGrpcShowCollectionsRequest();

#pragma warning disable CS0612 // The server marks ShowType as obsolete but still uses it.
        Assert.NotNull(grpc);
        Assert.Equal(Grpc.ShowType.All, grpc.Type);
#pragma warning restore CS0612
    }

    [Fact]
    public void ListCollections_maps_only_show_loaded()
    {
        var request = new ListCollectionsReq { OnlyShowLoaded = true };

        Grpc.ShowCollectionsRequest grpc = request.ToGrpcShowCollectionsRequest();

#pragma warning disable CS0612 // The server marks ShowType as obsolete but still uses it.
        Assert.Equal(Grpc.ShowType.InMemory, grpc.Type);
#pragma warning restore CS0612
    }

    [Fact]
    public void ReleaseCollection_maps_collection_name()
    {
        var request = new ReleaseCollectionReq { CollectionName = "book" };

        Grpc.ReleaseCollectionRequest grpc = request.ToGrpcReleaseCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
    }

    [Fact]
    public void LoadCollection_maps_priority_to_load_params()
    {
        var request = new LoadCollectionReq { CollectionName = "book", Priority = "Low" };

        Grpc.LoadCollectionRequest grpc = request.ToGrpcLoadCollectionRequest();

        Assert.Equal("low", grpc.LoadParams["load_priority"]);
    }

    [Fact]
    public void LoadCollection_omits_load_params_when_priority_not_set()
    {
        var request = new LoadCollectionReq { CollectionName = "book" };

        Grpc.LoadCollectionRequest grpc = request.ToGrpcLoadCollectionRequest();

        Assert.Empty(grpc.LoadParams);
    }

    [Fact]
    public void ReleaseCollection_throws_when_name_blank()
    {
        var request = new ReleaseCollectionReq { CollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcReleaseCollectionRequest());
    }

    [Fact]
    public void RenameCollection_maps_old_and_new_name()
    {
        var request = new RenameCollectionReq { CollectionName = "old_book", NewCollectionName = "new_book" };

        Grpc.RenameCollectionRequest grpc = request.ToGrpcRenameCollectionRequest();

        Assert.Equal("old_book", grpc.OldName);
        Assert.Equal("new_book", grpc.NewName);
    }

    [Fact]
    public void RenameCollection_throws_when_new_name_blank()
    {
        var request = new RenameCollectionReq { CollectionName = "old_book", NewCollectionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRenameCollectionRequest());
    }
}
