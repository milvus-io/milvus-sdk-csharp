using Xunit;

using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Response;

[Trait("Category", "Unit")]
public class CollectionRespTests
{
    [Fact]
    public void DescribeCollection_maps_all_public_properties()
    {
        var response = new Grpc.DescribeCollectionResponse
        {
            CollectionID = 100,
            Schema = new Grpc.CollectionSchema { Name = "book" },
            ShardsNum = 2,
            ConsistencyLevel = Grpc.ConsistencyLevel.Bounded,
            CreatedTimestamp = 12345678,
            Aliases = { "book_alias", "books" }
        };
        response.Properties.Add(new Grpc.KeyValuePair { Key = "collection.ttl.seconds", Value = "3600" });

        DescribeCollectionResp resp = DescribeCollectionResp.FromGrpc(response);

        Assert.Equal(100L, resp.CollectionId);
        Assert.Equal("book", resp.CollectionName);
        Assert.Equal("book", resp.Schema.Name);
        Assert.Equal(2, resp.ShardsNum);
        Assert.Equal(ConsistencyLevel.BoundedStaleness, resp.ConsistencyLevel);
        Assert.Equal(12345678UL, resp.CreatedTimestamp);
        Assert.Equal(new[] { "book_alias", "books" }, resp.Aliases);
        Assert.Equal("3600", resp.Properties["collection.ttl.seconds"]);
    }

    [Fact]
    public void DescribeCollection_maps_update_timestamp()
    {
        var response = new Grpc.DescribeCollectionResponse
        {
            Schema = new Grpc.CollectionSchema { Name = "book" },
            UpdateTimestamp = 987654
        };

        DescribeCollectionResp resp = DescribeCollectionResp.FromGrpc(response);

        Assert.Equal(987654UL, resp.UpdateTimestamp);
    }

    [Fact]
    public void ConvertSchema_restores_field_properties_and_type_params()
    {
        var grpcSchema = new Grpc.CollectionSchema
        {
            Name = "book",
            Description = "books collection",
            EnableDynamicField = true,
            Fields =
            {
                new Grpc.FieldSchema
                {
                    Name = "id",
                    DataType = Grpc.DataType.Int64,
                    IsPrimaryKey = true,
                    AutoID = true
                },
                new Grpc.FieldSchema
                {
                    Name = "title",
                    DataType = Grpc.DataType.VarChar,
                    Description = "book title",
                    Nullable = true,
                    DefaultValue = new Grpc.ValueField { StringData = "N/A" },
                    TypeParams =
                    {
                        new Grpc.KeyValuePair { Key = "max_length", Value = "100" },
                        new Grpc.KeyValuePair { Key = "enable_analyzer", Value = "true" },
                        new Grpc.KeyValuePair { Key = "analyzer_params", Value = "{\"type\":\"english\"}" }
                    }
                },
                new Grpc.FieldSchema
                {
                    Name = "tags",
                    DataType = Grpc.DataType.Array,
                    ElementType = Grpc.DataType.Int64,
                    IsFunctionOutput = true,
                    TypeParams =
                    {
                        new Grpc.KeyValuePair { Key = "max_capacity", Value = "8" }
                    }
                },
                new Grpc.FieldSchema
                {
                    Name = "embedding",
                    DataType = Grpc.DataType.FloatVector,
                    TypeParams =
                    {
                        new Grpc.KeyValuePair { Key = "dim", Value = "128" }
                    }
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);

        Assert.Equal("book", schema.Name);
        Assert.Equal("books collection", schema.Description);
        Assert.True(schema.EnableDynamicFields);
        Assert.Equal(4, schema.Fields.Count);

        FieldSchema id = schema.Fields[0];
        Assert.Equal("id", id.Name);
        Assert.Equal(DataType.Int64, id.DataType);
        Assert.True(id.IsPrimaryKey);
        Assert.True(id.AutoId);
        Assert.Null(id.ElementDataType);
        Assert.False(id.Nullable);

        FieldSchema title = schema.Fields[1];
        Assert.Equal("title", title.Name);
        Assert.Equal(DataType.VarChar, title.DataType);
        Assert.Equal("book title", title.Description);
        Assert.True(title.Nullable);
        Assert.Equal("N/A", title.DefaultValue);
        Assert.Equal(100, title.MaxLength);
        Assert.True(title.EnableAnalyzer);
        Assert.Equal("english", title.AnalyzerParams!["type"]);

        FieldSchema tags = schema.Fields[2];
        Assert.Equal(DataType.Array, tags.DataType);
        Assert.Equal(DataType.Int64, tags.ElementDataType);
        Assert.True(tags.IsFunctionOutput);        Assert.Equal(8, tags.MaxCapacity);

        FieldSchema embedding = schema.Fields[3];
        Assert.Equal(DataType.FloatVector, embedding.DataType);
        Assert.Equal(128, embedding.Dimension);
    }

    [Fact]
    public void ConvertSchema_deserializes_analyzer_params_into_typed_values()
    {
        var grpcSchema = new Grpc.CollectionSchema
        {
            Name = "book",
            Fields =
            {
                new Grpc.FieldSchema
                {
                    Name = "title",
                    DataType = Grpc.DataType.VarChar,
                    TypeParams =
                    {
                        new Grpc.KeyValuePair
                        {
                            Key = "analyzer_params",
                            Value = "{\"type\":\"english\",\"case_sensitive\":false,\"max_length\":3,\"nested\":{\"k\":\"v\"}}"
                        }
                    }
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);
        IReadOnlyDictionary<string, object> analyzerParams = schema.Fields[0].AnalyzerParams!;

        Assert.Equal("english", analyzerParams["type"]);
        Assert.Equal(false, analyzerParams["case_sensitive"]);
        Assert.Equal(3L, analyzerParams["max_length"]);
        Assert.Equal("v", ((Dictionary<string, object>)analyzerParams["nested"])["k"]);
    }

    [Fact]
    public void ConvertSchema_restores_default_values_and_blank_description_as_null()
    {
        var grpcSchema = new Grpc.CollectionSchema
        {
            Name = "events",
            Description = "",
            Fields =
            {
                new Grpc.FieldSchema
                {
                    Name = "count",
                    DataType = Grpc.DataType.Int64,
                    DefaultValue = new Grpc.ValueField { LongData = 7 }
                },
                new Grpc.FieldSchema
                {
                    Name = "active",
                    DataType = Grpc.DataType.Bool,
                    DefaultValue = new Grpc.ValueField { BoolData = true }
                },
                new Grpc.FieldSchema
                {
                    Name = "score",
                    DataType = Grpc.DataType.Double,
                    DefaultValue = new Grpc.ValueField { DoubleData = 1.5 }
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);

        Assert.Null(schema.Description);
        Assert.Equal(7L, (long)schema.Fields[0].DefaultValue!);
        Assert.True((bool)schema.Fields[1].DefaultValue!);
        Assert.Equal(1.5, (double)schema.Fields[2].DefaultValue!);
    }

    [Fact]
    public void DescribeCollection_decodes_timestamptz_default_value_as_iso_string()
    {
        var grpcSchema = new Grpc.CollectionSchema
        {
            Fields =
            {
                new Grpc.FieldSchema
                {
                    Name = "created_at",
                    DataType = Grpc.DataType.Timestamptz,
                    DefaultValue = new Grpc.ValueField { TimestamptzData = 1_700_000_000_000_000 } // epoch microseconds
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);

        // The default surfaces as the same ISO-8601 form used by query/search row decoding, not a raw epoch.
        Assert.Equal("2023-11-14T22:13:20.0000000+00:00", schema.Fields[0].DefaultValue);
    }

    [Fact]
    public void DescribeCollection_uses_string_slot_for_timestamptz_default()
    {
        // The proxy rewrites timestamptz defaults to StringData as an ISO-8601 string; that slot must win
        // over the (zero) epoch-microsecond slot.
        var grpcSchema = new Grpc.CollectionSchema
        {
            Fields =
            {
                new Grpc.FieldSchema
                {
                    Name = "created_at",
                    DataType = Grpc.DataType.Timestamptz,
                    DefaultValue = new Grpc.ValueField { StringData = "2025-03-20T10:30:00Z" }
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);

        Assert.Equal("2025-03-20T10:30:00Z", schema.Fields[0].DefaultValue);
    }

    [Fact]
    public void DescribeCollection_decodes_json_default_value_from_string_slot()
    {
        // JSON defaults travel in the StringData slot (the create path encodes them as a JSON string); the read
        // path must surface them so write/read stay symmetric.
        var grpcSchema = new Grpc.CollectionSchema
        {
            Fields =
            {
                new Grpc.FieldSchema
                {
                    Name = "meta",
                    DataType = Grpc.DataType.Json,
                    DefaultValue = new Grpc.ValueField { StringData = "{\"k\":1}" }
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);

        Assert.Equal("{\"k\":1}", schema.Fields[0].DefaultValue);
    }

    [Fact]
    public void DescribeReplicas_maps_replica_info_and_shards()
    {
        var response = new Grpc.GetReplicasResponse
        {
            Replicas =
            {
                new Grpc.ReplicaInfo
                {
                    ReplicaID = 5,
                    CollectionID = 100,
                    PartitionIds = { 1, 2 },
                    ShardReplicas =
                    {
                        new Grpc.ShardReplica
                        {
                            LeaderID = 10,
                            LeaderAddr = "host-1:19530",
                            DmChannelName = "by-dev-rootcoord-dml_0",
                            NodeIds = { 10, 11 }
                        }
                    },
                    NodeIds = { 10, 11, 12 },
                    ResourceGroupName = "rg-a",
                    NumOutboundNode = { ["rg-b"] = 1 }
                }
            }
        };

        DescribeReplicasResp resp = DescribeReplicasResp.FromGrpc(response);

        Assert.Single(resp.Replicas);

        ReplicaInfo replica = resp.Replicas[0];
        Assert.Equal(5L, replica.ReplicaId);
        Assert.Equal(100L, replica.CollectionId);
        Assert.Equal(new[] { 1L, 2L }, replica.PartitionIds);
        Assert.Equal(new[] { 10L, 11L, 12L }, replica.NodeIds);
        Assert.Equal("rg-a", replica.ResourceGroupName);
        Assert.Equal(1, replica.NumOutboundNode["rg-b"]);

        ShardReplica shard = replica.ShardReplicas[0];
        Assert.Equal(10L, shard.LeaderId);
        Assert.Equal("host-1:19530", shard.LeaderAddress);
        Assert.Equal("by-dev-rootcoord-dml_0", shard.DmChannelName);
        Assert.Equal(new[] { 10L, 11L }, shard.NodeIds);
    }

    [Fact]
    public void GetCollectionStats_maps_row_count()
    {
        var response = new Grpc.GetCollectionStatisticsResponse
        {
            Stats =
            {
                new Grpc.KeyValuePair { Key = "row_count", Value = "150" },
                new Grpc.KeyValuePair { Key = "custom", Value = "abc" }
            }
        };

        GetCollectionStatsResp resp = GetCollectionStatsResp.FromGrpc("book", response);

        Assert.Equal("book", resp.Name);
        Assert.Equal(150L, resp.RowCount);
        Assert.Equal(2, resp.Stats.Count);
        Assert.Equal("150", resp.Stats["row_count"]);
        Assert.Equal("abc", resp.Stats["custom"]);
    }

    [Fact]
    public void GetCollectionStats_defaults_row_count_when_missing()
    {
        var response = new Grpc.GetCollectionStatisticsResponse
        {
            Stats =
            {
                new Grpc.KeyValuePair { Key = "other", Value = "ignored" }
            }
        };

        GetCollectionStatsResp resp = GetCollectionStatsResp.FromGrpc("book", response);

        Assert.Equal("book", resp.Name);
        Assert.Equal(0L, resp.RowCount);
    }

    [Fact]
    public void GetLoadState_maps_state()
    {
        var response = new Grpc.GetLoadStateResponse { State = Grpc.LoadState.Loaded };

        GetLoadStateResp resp = GetLoadStateResp.FromGrpc(response);

        Assert.Equal(LoadState.Loaded, resp.State);
    }

    [Fact]
    public void GetLoadState_maps_not_exist_state()
    {
        var response = new Grpc.GetLoadStateResponse { State = Grpc.LoadState.NotExist };

        GetLoadStateResp resp = GetLoadStateResp.FromGrpc(response);

        Assert.Equal(LoadState.NotExist, resp.State);
    }

    [Fact]
    public void HasCollection_maps_has_flag()
    {
        var response = new Grpc.BoolResponse { Value = true };

        HasCollectionResp resp = HasCollectionResp.FromGrpc(response);

        Assert.True(resp.Has);
    }

    [Fact]
    public void ListCollections_maps_names_and_ids()
    {
        var response = new Grpc.ShowCollectionsResponse
        {
            CollectionNames = { "book", "user" },
            CollectionIds = { 100, 200 },
            CreatedTimestamps = { 1000, 2000 },
            CreatedUtcTimestamps = { 1001, 2001 }
        };
#pragma warning disable CS0612
        response.InMemoryPercentages.Add(50);
        response.InMemoryPercentages.Add(100);
#pragma warning restore CS0612

        ListCollectionsResp resp = ListCollectionsResp.FromGrpc(response);

        Assert.Equal(new[] { "book", "user" }, resp.CollectionNames);
        Assert.Equal(new[] { 100L, 200L }, resp.CollectionIds);
        Assert.Equal(new[] { 1000UL, 2000UL }, resp.CreatedTimestamps);
        Assert.Equal(new[] { 1001UL, 2001UL }, resp.CreatedUtcTimestamps);
#pragma warning disable CS0612
        Assert.Equal(new[] { 50L, 100L }, resp.InMemoryPercentages);
#pragma warning restore CS0612
    }

    [Fact]
    public void DescribeCollection_reads_struct_max_capacity_once_and_skips_no_element_type()
    {
        var grpcSchema = new Grpc.CollectionSchema
        {
            StructArrayFields =
            {
                new Grpc.StructArrayFieldSchema
                {
                    Name = "st",
                    Fields =
                    {
                        new Grpc.FieldSchema
                        {
                            Name = "a",
                            ElementType = Grpc.DataType.Int32,
                            TypeParams =
                            {
                                new Grpc.KeyValuePair { Key = "max_capacity", Value = "8" }
                            }
                        },
                        // Missing element_type is malformed: must be skipped, not mislabeled as int8.
                        new Grpc.FieldSchema { Name = "broken", TypeParams = { new Grpc.KeyValuePair { Key = "max_capacity", Value = "8" } } }
                    }
                }
            }
        };

        CollectionSchema schema = DescribeCollectionResp.ConvertSchema(grpcSchema);

        StructFieldSchema structField = Assert.Single(schema.StructFields);
        Assert.Equal(8, structField.MaxCapacity);
        Assert.Single(structField.Fields);
        Assert.Equal("a", structField.Fields[0].Name);
    }
}
