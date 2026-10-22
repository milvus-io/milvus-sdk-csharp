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
#pragma warning disable CS0618 // AddCollectionFunctionReq/DropCollectionFunctionReq are deprecated but their conversions are still exercised here.
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
#pragma warning restore CS0618

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
#pragma warning disable CS0618 // DropCollectionFunctionReq is deprecated but its conversion is still exercised here.
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
#pragma warning restore CS0618

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

    [Fact]
    public void AddCollectionStructField_maps_struct_field_and_nullable()
    {
        var structField = new StructFieldSchema("extra_meta", "additional metadata")
        {
            MaxCapacity = 8,
            Nullable = true,
            Fields =
            {
                new FieldSchema("int32", DataType.Int32),
                new FieldSchema("tag", DataType.VarChar) { MaxLength = 16 },
                new FieldSchema("vector", DataType.FloatVector) { Dimension = 4 }
            }
        };

        var request = new AddCollectionStructFieldReq
        {
            CollectionName = "book",
            StructField = structField
        };

        Grpc.AddCollectionStructFieldRequest grpc = request.ToGrpcAddCollectionStructFieldRequest();

        Assert.Equal("book", grpc.CollectionName);
        Grpc.StructArrayFieldSchema grpcStruct = grpc.StructArrayFieldSchema;
        Assert.Equal("extra_meta", grpcStruct.Name);
        Assert.Equal("additional metadata", grpcStruct.Description);
        Assert.True(grpcStruct.Nullable);
        Assert.Equal(3, grpcStruct.Fields.Count);

        // Scalar sub-field encoded as Array with the real type in element_type, the struct's max_capacity as a
        // type param, and the struct's nullable applied to every sub-field.
        Grpc.FieldSchema intSub = grpcStruct.Fields[0];
        Assert.Equal(Grpc.DataType.Array, intSub.DataType);
        Assert.Equal(Grpc.DataType.Int32, intSub.ElementType);
        Assert.True(intSub.Nullable);
        Assert.Contains(intSub.TypeParams, p => p.Key == "max_capacity" && p.Value == "8");

        Grpc.FieldSchema varcharSub = grpcStruct.Fields[1];
        Assert.Contains(varcharSub.TypeParams, p => p.Key == "max_length" && p.Value == "16");

        // Vector sub-field encoded as ArrayOfVector with dim.
        Grpc.FieldSchema vectorSub = grpcStruct.Fields[2];
        Assert.Equal(Grpc.DataType.ArrayOfVector, vectorSub.DataType);
        Assert.Equal(Grpc.DataType.FloatVector, vectorSub.ElementType);
        Assert.Contains(vectorSub.TypeParams, p => p.Key == "dim" && p.Value == "4");
    }

    [Fact]
    public void AddCollectionStructField_throws_when_collection_blank()
    {
        var request = new AddCollectionStructFieldReq
        {
            CollectionName = " ",
            StructField = new StructFieldSchema("st") { Nullable = true, MaxCapacity = 4 }
        };
        Assert.Throws<ArgumentException>(() => request.ToGrpcAddCollectionStructFieldRequest());
    }

    [Fact]
    public void AddCollectionStructField_throws_when_struct_null()
    {
        var request = new AddCollectionStructFieldReq { CollectionName = "book", StructField = null! };
        Assert.Throws<ArgumentNullException>(() => request.ToGrpcAddCollectionStructFieldRequest());
    }

    [Fact]
    public void AddCollectionStructField_rejects_non_nullable_struct()
    {
        var request = new AddCollectionStructFieldReq
        {
            CollectionName = "book",
            StructField = new StructFieldSchema("st")
            {
                MaxCapacity = 4,
                Fields = { new FieldSchema("x", DataType.Int32) }
            }
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcAddCollectionStructFieldRequest());
        Assert.Contains("must be nullable", ex.Message);
    }

    [Fact]
    public void AddCollectionStructField_nullable_struct_with_nullable_sub_field_inherits_nullable()
    {
        // AddCollectionStructField requires the struct to be nullable, so the "sub-field nullable without a
        // nullable struct" rejection is not reachable on this API (the struct-level check fires first). A
        // nullable struct accepts a nullable sub-field and propagates the struct's nullable to it on the wire.
        var request = new AddCollectionStructFieldReq
        {
            CollectionName = "book",
            StructField = new StructFieldSchema("st")
            {
                MaxCapacity = 4,
                Nullable = true,
                Fields = { new FieldSchema("x", DataType.Int32) { Nullable = true } }
            }
        };

        Grpc.AddCollectionStructFieldRequest grpc = request.ToGrpcAddCollectionStructFieldRequest();

        Assert.True(grpc.StructArrayFieldSchema.Nullable);
        Assert.True(grpc.StructArrayFieldSchema.Fields.Single().Nullable);
    }

    [Fact]
    public void AddCollectionStructField_carries_struct_type_params()
    {
        var request = new AddCollectionStructFieldReq
        {
            CollectionName = "book",
            StructField = new StructFieldSchema("extra_meta")
            {
                MaxCapacity = 8,
                Nullable = true,
                TypeParams =
                {
                    ["warmup"] = "WarmUpAfterIndexBuild",
                    ["mmap.enabled"] = "true"
                },
                Fields = { new FieldSchema("rating", DataType.Int32) }
            }
        };

        Grpc.AddCollectionStructFieldRequest grpc = request.ToGrpcAddCollectionStructFieldRequest();

        Assert.Equal("WarmUpAfterIndexBuild", grpc.StructArrayFieldSchema.TypeParams.Single(p => p.Key == "warmup").Value);
        Assert.Equal("true", grpc.StructArrayFieldSchema.TypeParams.Single(p => p.Key == "mmap.enabled").Value);
    }

    [Fact]
    public void DropCollectionField_maps_field_name()
    {
        var request = new DropCollectionFieldReq { CollectionName = "book", FieldName = "title" };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(Grpc.AlterCollectionSchemaRequest.Types.DropRequest.IdentifierOneofCase.FieldName,
            grpc.Action.DropRequest.IdentifierCase);
        Assert.Equal("title", grpc.Action.DropRequest.FieldName);
        Assert.False(grpc.Action.DropRequest.DropFunctionOutputFields);
    }

    [Fact]
    public void DropCollectionField_maps_field_id()
    {
        var request = new DropCollectionFieldReq { CollectionName = "book", FieldId = 101 };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Assert.Equal(Grpc.AlterCollectionSchemaRequest.Types.DropRequest.IdentifierOneofCase.FieldId,
            grpc.Action.DropRequest.IdentifierCase);
        Assert.Equal(101L, grpc.Action.DropRequest.FieldId);
    }

    [Fact]
    public void DropCollectionField_rejects_both_and_neither_selector()
    {
        var both = new DropCollectionFieldReq { CollectionName = "book", FieldName = "title", FieldId = 101 };
        Assert.Throws<ArgumentException>(() => both.ToGrpcAlterCollectionSchemaRequest());

        var neither = new DropCollectionFieldReq { CollectionName = "book" };
        Assert.Throws<ArgumentException>(() => neither.ToGrpcAlterCollectionSchemaRequest());
    }

    [Fact]
    public void DropCollectionField_rejects_non_positive_field_id()
    {
        // A provided but non-positive FieldId must fail with a clear message rather than masquerading as
        // "no selector given" (the wire contract requires a positive field id).
        var negativeId = new DropCollectionFieldReq { CollectionName = "book", FieldId = -1 };
        ArgumentException negative = Assert.Throws<ArgumentException>(() => negativeId.ToGrpcAlterCollectionSchemaRequest());
        Assert.Contains("positive", negative.Message);

        var zeroId = new DropCollectionFieldReq { CollectionName = "book", FieldId = 0 };
        ArgumentException zero = Assert.Throws<ArgumentException>(() => zeroId.ToGrpcAlterCollectionSchemaRequest());
        Assert.Contains("positive", zero.Message);
    }

    [Fact]
    public void AddFunctionField_maps_bm25_function_field_and_bound_index()
    {
        var index = new IndexParam("sparse", "sparse_idx", IndexType.SparseInvertedIndex, MetricType.Bm25);
        index.ExtraParams["drop_ratio_build"] = "0.2";

        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse", "bm25 scoring"),
            IndexParam = index
        };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Assert.Equal("book", grpc.CollectionName);
        Grpc.AlterCollectionSchemaRequest.Types.AddRequest addRequest = grpc.Action.AddRequest;
        Grpc.AlterCollectionSchemaRequest.Types.FieldInfo fieldInfo = Assert.Single(addRequest.FieldInfos);

        Grpc.FieldSchema fieldSchema = fieldInfo.FieldSchema;
        Assert.Equal("sparse", fieldSchema.Name);
        Assert.Equal(Grpc.DataType.SparseFloatVector, fieldSchema.DataType);
        Assert.True(fieldSchema.IsFunctionOutput);
        Assert.Equal("sparse_idx", fieldInfo.IndexName);
        Assert.Equal("SPARSE_INVERTED_INDEX", fieldInfo.ExtraParams.Single(p => p.Key == "index_type").Value);
        Assert.Equal("BM25", fieldInfo.ExtraParams.Single(p => p.Key == "metric_type").Value);
        Assert.Equal("0.2", fieldInfo.ExtraParams.Single(p => p.Key == "drop_ratio_build").Value);

        Grpc.FunctionSchema function = Assert.Single(addRequest.FuncSchema);
        Assert.Equal("bm25_fn", function.Name);
        Assert.Equal(Grpc.FunctionType.Bm25, function.Type);
        Assert.Equal("bm25 scoring", function.Description);
    }

    [Fact]
    public void AddFunctionField_maps_minhash_function_field()
    {
        var index = new IndexParam("minhash_vec", "minhash_idx", IndexType.MinHashLsh, MetricType.MhJaccard);

        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = new FieldSchema("minhash_vec", DataType.BinaryVector) { Dimension = 512 },
            Function = new FunctionSchema("minhash_fn", FunctionType.MinHash,
                new[] { "text" }, new[] { "minhash_vec" }),
            IndexParam = index
        };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Grpc.AlterCollectionSchemaRequest.Types.FieldInfo fieldInfo = Assert.Single(grpc.Action.AddRequest.FieldInfos);
        Assert.Equal(Grpc.DataType.BinaryVector, fieldInfo.FieldSchema.DataType);
        Assert.Equal("512", fieldInfo.FieldSchema.TypeParams.Single(p => p.Key == "dim").Value);
        Assert.Equal("MINHASH_LSH", fieldInfo.ExtraParams.Single(p => p.Key == "index_type").Value);
        Assert.Equal("MHJACCARD", fieldInfo.ExtraParams.Single(p => p.Key == "metric_type").Value);
        Assert.Equal(Grpc.FunctionType.MinHash, Assert.Single(grpc.Action.AddRequest.FuncSchema).Type);
    }

    [Fact]
    public void AddFunctionField_honors_raw_metric_type_when_typed_metric_unset()
    {
        // When the typed MetricType is left unset, a raw "metric_type" supplied only via ExtraParams must ride
        // through (mirroring the Java SDK and the CreateIndexReq path), instead of being silently dropped.
        var index = new IndexParam("sparse", "sparse_idx", IndexType.SparseInvertedIndex);
        index.ExtraParams["metric_type"] = "BM25";
        index.ExtraParams["drop_ratio_build"] = "0.2";

        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"),
            IndexParam = index
        };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Grpc.AlterCollectionSchemaRequest.Types.FieldInfo fieldInfo = Assert.Single(grpc.Action.AddRequest.FieldInfos);
        Assert.Equal("SPARSE_INVERTED_INDEX", fieldInfo.ExtraParams.Single(p => p.Key == "index_type").Value);
        Assert.Equal("BM25", fieldInfo.ExtraParams.Single(p => p.Key == "metric_type").Value);
        Assert.Equal("0.2", fieldInfo.ExtraParams.Single(p => p.Key == "drop_ratio_build").Value);
    }

    [Fact]
    public void AddFunctionField_typed_metric_skips_raw_metric_type_duplicate()
    {
        // When the typed MetricType is set it wins: the raw "metric_type" duplicate is skipped so the request
        // carries a single metric_type entry.
        var index = new IndexParam("sparse", "sparse_idx", IndexType.SparseInvertedIndex, MetricType.Bm25);
        index.ExtraParams["metric_type"] = "IP";

        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"),
            IndexParam = index
        };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Grpc.AlterCollectionSchemaRequest.Types.FieldInfo fieldInfo = Assert.Single(grpc.Action.AddRequest.FieldInfos);
        Assert.Equal("BM25", fieldInfo.ExtraParams.Single(p => p.Key == "metric_type").Value);
    }

    [Fact]
    public void AddFunctionField_rejects_unsupported_function_type()
    {
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = new FunctionSchema("embed_fn", FunctionType.TextEmbedding,
                new[] { "text" }, new[] { "sparse" }),
            IndexParam = new IndexParam("sparse", null, IndexType.SparseInvertedIndex, MetricType.Bm25)
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
        Assert.Contains("only supports", ex.Message);
    }

    [Fact]
    public void AddFunctionField_rejects_mismatched_output_type()
    {
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateFloatVector("dense", 4),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "dense"),
            IndexParam = new IndexParam("dense", null, IndexType.Flat, MetricType.Cosine)
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
        Assert.Contains("requires a SparseFloatVector output field", ex.Message);
    }

    [Fact]
    public void AddFunctionField_rejects_missing_bound_index_type()
    {
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"),
            IndexParam = new IndexParam("sparse")
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
    }

    [Fact]
    public void AddFunctionField_rejects_mismatched_index_field()
    {
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"),
            IndexParam = new IndexParam("other", null, IndexType.SparseInvertedIndex, MetricType.Bm25)
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
    }

    [Fact]
    public void AddFunctionField_rejects_missing_function_or_index()
    {
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"),
            IndexParam = null!
        };
        Assert.Throws<ArgumentNullException>(() => request.ToGrpcAlterCollectionSchemaRequest());
    }

    [Fact]
    public void AddFunctionField_rejects_analyzer_settings_on_output_field()
    {
        // Analyzers belong on the function's input text field, not on its vector output field; settings on the
        // output field must be rejected client-side rather than forwarded for an opaque server error.
        var outputField = new FieldSchema("sparse", DataType.SparseFloatVector) { EnableAnalyzer = true };
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = outputField,
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "sparse"),
            IndexParam = new IndexParam("sparse", null, IndexType.SparseInvertedIndex, MetricType.Bm25)
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
        Assert.Contains("does not support text analyzers", ex.Message);
    }

    [Fact]
    public void AddFunctionField_rejects_output_field_not_in_function_outputs()
    {
        // The added field must be one of the function's output fields; a mismatch is rejected client-side
        // instead of failing with an opaque server error.
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25_fn", "text", "other_out"),
            IndexParam = new IndexParam("sparse", null, IndexType.SparseInvertedIndex, MetricType.Bm25)
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
        Assert.Contains("output field names", ex.Message);
    }

    [Fact]
    public void AddFunctionField_rejects_binary_vector_dimension_not_multiple_of_8()
    {
        // MinHash output fields are BinaryVector; the dimension must be a multiple of 8 (and at least 8),
        // matching the create-collection path validation.
        var request = new AddFunctionFieldReq
        {
            CollectionName = "book",
            Field = new FieldSchema("minhash_vec", DataType.BinaryVector) { Dimension = 12 },
            Function = new FunctionSchema("minhash_fn", FunctionType.MinHash,
                new[] { "text" }, new[] { "minhash_vec" }),
            IndexParam = new IndexParam("minhash_vec", null, IndexType.MinHashLsh, MetricType.MhJaccard)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => request.ToGrpcAlterCollectionSchemaRequest());
    }

    [Fact]
    public void DropFunctionField_maps_function_name_and_drops_output_fields()
    {
        var request = new DropFunctionFieldReq { CollectionName = "book", FunctionName = "bm25_fn" };

        Grpc.AlterCollectionSchemaRequest grpc = request.ToGrpcAlterCollectionSchemaRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(Grpc.AlterCollectionSchemaRequest.Types.DropRequest.IdentifierOneofCase.FunctionName,
            grpc.Action.DropRequest.IdentifierCase);
        Assert.Equal("bm25_fn", grpc.Action.DropRequest.FunctionName);
        Assert.True(grpc.Action.DropRequest.DropFunctionOutputFields);
    }

    [Fact]
    public void DropFunctionField_throws_when_function_name_blank()
    {
        var request = new DropFunctionFieldReq { CollectionName = "book", FunctionName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcAlterCollectionSchemaRequest());
    }
}
