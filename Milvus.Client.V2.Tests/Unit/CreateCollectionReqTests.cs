using Xunit;

using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit;

[Trait("Category", "Unit")]
public class CreateCollectionReqTests
{
    [Fact]
    public void ToGrpc_maps_schema_fields_and_type_params()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            ConsistencyLevel = ConsistencyLevel.Strong,
            ShardsNum = 2,
            Schema = new CollectionSchema
            {
                Name = "book",
                Description = "books",
                EnableDynamicFields = true,
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("title", maxLength: 100),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4)
                }
            }
        };

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal((int)ConsistencyLevel.Strong, (int)grpc.ConsistencyLevel);
        Assert.Equal(2, grpc.ShardsNum);

        Grpc.CollectionSchema schema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);
        Assert.Equal("book", schema.Name);
        Assert.Equal("books", schema.Description);
        Assert.True(schema.EnableDynamicField);
        Assert.Equal(3, schema.Fields.Count);

        Grpc.FieldSchema title = schema.Fields.Single(f => f.Name == "title");
        Assert.Equal((int)DataType.VarChar, (int)title.DataType);
        Assert.Equal("100", title.TypeParams.Single(p => p.Key == "max_length").Value);

        Grpc.FieldSchema vector = schema.Fields.Single(f => f.Name == "embedding");
        Assert.Equal((int)DataType.FloatVector, (int)vector.DataType);
        Assert.Equal("4", vector.TypeParams.Single(p => p.Key == "dim").Value);
    }

    [Fact]
    public void ToGrpc_maps_functions_properties_and_num_partitions()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("title", maxLength: 100),
                    FieldSchema.CreateFloatVector("embedding", dimension: 4),
                    FieldSchema.CreateSparseFloatVector("sparse")
                },
                Functions =
                {
                    FunctionSchema.CreateBm25("bm25", "title", "sparse", "full-text search")
                }
            },
            NumPartitions = 8
        };
        request.Properties["collection.ttl.seconds"] = "3600";

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();

        Assert.Equal(8, grpc.NumPartitions);
        Grpc.KeyValuePair property = Assert.Single(grpc.Properties);
        Assert.Equal("collection.ttl.seconds", property.Key);
        Assert.Equal("3600", property.Value);

        Grpc.CollectionSchema schema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);
        Grpc.FunctionSchema function = Assert.Single(schema.Functions);
        Assert.Equal("bm25", function.Name);
        Assert.Equal((int)FunctionType.Bm25, (int)function.Type);
        Assert.Equal(new[] { "title" }, function.InputFieldNames);
        Assert.Equal(new[] { "sparse" }, function.OutputFieldNames);
        Assert.Equal("full-text search", function.Description);
    }

    [Fact]
    public void ToGrpc_throws_when_collection_name_blank()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = " ",
            Schema = new CollectionSchema { Fields = { new FieldSchema("id", DataType.Int64) } }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void ToGrpc_throws_when_schema_missing()
    {
        var request = new CreateCollectionReq { CollectionName = "book" };

        Assert.Throws<ArgumentNullException>(() => request.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void ToGrpc_encodes_json_default_values_as_string()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true)
                    {
                        DefaultValue = 1
                    },
                    new FieldSchema("meta", DataType.Json)
                    {
                        DefaultValue = "{\"a\":1}"
                    }
                }
            }
        };

        // JSON defaults travel in the string_data slot (matching the Java SDK, which serializes JsonObject
        // via toString), so the SDK must encode rather than reject them.
        var grpc = request.ToGrpcCreateCollectionRequest();
        var schema = Milvus.Client.Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);
        Milvus.Client.Grpc.FieldSchema meta = schema.Fields.Single(f => f.Name == "meta");
        Assert.Equal("{\"a\":1}", meta.DefaultValue.StringData);
    }

    [Fact]
    public void IndexDesc_to_create_index_req_carries_fields()
    {
        var index = new IndexParam("embedding", "idx1", IndexType.Hnsw, MetricType.Cosine);
        index.ExtraParams["M"] = "16";

        var request = new CreateIndexReq { CollectionName = "book", Indexes = [index] };
        Grpc.CreateIndexRequest grpc = request.ToGrpcCreateIndexRequest(index);

        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal("embedding", grpc.FieldName);
        Assert.Equal("idx1", grpc.IndexName);
        Assert.Equal("HNSW", grpc.ExtraParams.Single(p => p.Key == "index_type").Value);
        Assert.Equal("COSINE", grpc.ExtraParams.Single(p => p.Key == "metric_type").Value);
        Assert.Equal("16", grpc.ExtraParams.Single(p => p.Key == "M").Value);
    }

    [Fact]
    public void ToGrpc_encodes_struct_fields_in_schema()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 8,
                        Fields =
                        {
                            new FieldSchema("int32", DataType.Int32),
                            new FieldSchema("varchar", DataType.VarChar) { MaxLength = 16 },
                            new FieldSchema("vector", DataType.FloatVector) { Dimension = 4 }
                        }
                    }
                }
            }
        };

        Milvus.Client.Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        var schema = Milvus.Client.Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Single(schema.StructArrayFields);
        Milvus.Client.Grpc.StructArrayFieldSchema structField = schema.StructArrayFields[0];
        Assert.Equal("st", structField.Name);
        Assert.Equal(3, structField.Fields.Count);

        // Scalar sub-field encoded as Array with the real type in element_type and max_capacity type param.
        Milvus.Client.Grpc.FieldSchema intSub = structField.Fields[0];
        Assert.Equal(Milvus.Client.Grpc.DataType.Array, intSub.DataType);
        Assert.Equal(Milvus.Client.Grpc.DataType.Int32, intSub.ElementType);
        Assert.Contains(intSub.TypeParams, p => p.Key == "max_capacity" && p.Value == "8");

        // Vector sub-field encoded as ArrayOfVector with dim.
        Milvus.Client.Grpc.FieldSchema vectorSub = structField.Fields[2];
        Assert.Equal(Milvus.Client.Grpc.DataType.ArrayOfVector, vectorSub.DataType);
        Assert.Equal(Milvus.Client.Grpc.DataType.FloatVector, vectorSub.ElementType);
        Assert.Contains(vectorSub.TypeParams, p => p.Key == "dim" && p.Value == "4");
        Assert.Contains(vectorSub.TypeParams, p => p.Key == "max_capacity" && p.Value == "8");
    }

    [Fact]
    public void ToGrpc_stamps_struct_nullable_and_inherits_to_sub_fields()
    {
        // A nullable struct is stamped on the wire and its nullable flag is applied to every sub-field (a struct
        // is nullable as a whole), matching the Java/Rust SDK encoding.
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 4,
                        Nullable = true,
                        Fields = { new FieldSchema("x", DataType.Int32) { Nullable = true } }
                    }
                }
            }
        };

        Milvus.Client.Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        var schema = Milvus.Client.Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);

        Milvus.Client.Grpc.StructArrayFieldSchema structField = Assert.Single(schema.StructArrayFields);
        Assert.True(structField.Nullable);
        Assert.True(structField.Fields.Single().Nullable);
    }

    [Fact]
    public void ToGrpc_rejects_nullable_sub_field_on_non_nullable_struct()
    {
        // A struct that is not nullable cannot have an individually nullable sub-field; this is the only path
        // (the create request) that reaches the sub-field-level rejection -- AddCollectionStructField requires a
        // nullable struct and fails at the struct-level check first.
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 4,
                        Fields = { new FieldSchema("x", DataType.Int32) { Nullable = true } }
                    }
                }
            }
        };

        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void ToGrpc_stamps_struct_type_params()
    {
        // Struct-level type_params (warmup, mmap.enabled) ride along on the wire, mirroring the Java SDK's
        // StructFieldSchema.typeParams and pymilvus struct params.
        var request = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 4,
                        TypeParams =
                        {
                            ["warmup"] = "WarmUpAfterIndexBuild",
                            ["mmap.enabled"] = "true"
                        },
                        Fields = { new FieldSchema("x", DataType.Int32) }
                    }
                }
            }
        };

        Milvus.Client.Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        var schema = Milvus.Client.Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);

        Milvus.Client.Grpc.StructArrayFieldSchema structField = Assert.Single(schema.StructArrayFields);
        Assert.Equal("WarmUpAfterIndexBuild", structField.TypeParams.Single(p => p.Key == "warmup").Value);
        Assert.Equal("true", structField.TypeParams.Single(p => p.Key == "mmap.enabled").Value);
    }

    [Fact]
    public void ToGrpc_rejects_struct_without_max_capacity_or_sub_fields()
    {
        var withoutCapacity = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields = { new StructFieldSchema("st") { Fields = { new FieldSchema("x", DataType.Int32) } } }
            }
        };
        Assert.Throws<ArgumentException>(() => withoutCapacity.ToGrpcCreateCollectionRequest());

        var withoutSubFields = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields = { new StructFieldSchema("st") { MaxCapacity = 4 } }
            }
        };
        Assert.Throws<ArgumentException>(() => withoutSubFields.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void ToGrpc_rejects_analyzer_configuration_on_struct_sub_fields()
    {
        // Struct sub-fields are encoded as Array/ArrayOfVector and do not accept text analyzers; the settings
        // must be rejected rather than silently dropped.
        var withAnalyzer = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 4,
                        Fields = { new FieldSchema("text", DataType.VarChar) { EnableAnalyzer = true } }
                    }
                }
            }
        };
        Assert.Throws<ArgumentException>(() => withAnalyzer.ToGrpcCreateCollectionRequest());

        var withAnalyzerParams = new CreateCollectionReq
        {
            CollectionName = "book",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 4,
                        Fields =
                        {
                            new FieldSchema("text", DataType.VarChar)
                            {
                                AnalyzerParams = new Dictionary<string, object> { ["type"] = "english" }
                            }
                        }
                    }
                }
            }
        };
        Assert.Throws<ArgumentException>(() => withAnalyzerParams.ToGrpcCreateCollectionRequest());
    }

    [Fact]
    public void ToGrpc_maps_external_source_and_spec()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "ext",
            Schema = new CollectionSchema
            {
                Fields = { new FieldSchema("id", DataType.Int64, isPrimaryKey: true) },
                ExternalSource = "s3://bucket/path",
                ExternalSpec = "{\"format\":\"parquet\"}"
            }
        };

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        Grpc.CollectionSchema schema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Equal("s3://bucket/path", schema.ExternalSource);
        Assert.Equal("{\"format\":\"parquet\"}", schema.ExternalSpec);
    }

    [Fact]
    public void ToGrpc_normalizes_mmap_enabled_to_mmap_dot_enabled()
    {
        var request = new CreateCollectionReq
        {
            CollectionName = "coll",
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("title", DataType.VarChar)
                    {
                        TypeParams = { ["mmap_enabled"] = "true" }
                    }
                },
                StructFields =
                {
                    new StructFieldSchema("st")
                    {
                        MaxCapacity = 4,
                        TypeParams = { ["mmap_enabled"] = "false" },
                        Fields =
                        {
                            new FieldSchema("txt", DataType.VarChar)
                            {
                                TypeParams = { ["mmap_enabled"] = "true" }
                            }
                        }
                    }
                }
            }
        };

        Grpc.CreateCollectionRequest grpc = request.ToGrpcCreateCollectionRequest();
        Grpc.CollectionSchema schema = Grpc.CollectionSchema.Parser.ParseFrom(grpc.Schema);

        Assert.Equal("true", schema.Fields.Single(f => f.Name == "title").TypeParams.Single(p => p.Key == "mmap.enabled").Value);
        Grpc.StructArrayFieldSchema structField = schema.StructArrayFields.Single(s => s.Name == "st");
        Assert.Equal("false", structField.TypeParams.Single(p => p.Key == "mmap.enabled").Value);
        Assert.Equal("true", structField.Fields.Single(f => f.Name == "txt").TypeParams.Single(p => p.Key == "mmap.enabled").Value);
    }
}
