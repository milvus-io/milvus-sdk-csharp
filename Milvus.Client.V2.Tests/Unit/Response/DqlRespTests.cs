using Xunit;

using Google.Protobuf;

using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Response;

[Trait("Category", "Unit")]
public class DqlRespTests
{
    [Fact]
    public void Mutation_parses_long_ids_and_counts()
    {
        var response = new Grpc.MutationResult
        {
            IDs = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L, 2L, 3L } } },
            InsertCnt = 2,
            DeleteCnt = 0,
            UpsertCnt = 1,
            Timestamp = 123456789
        };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Equal(new[] { 1L, 2L, 3L }, result.LongIds);
        Assert.Null(result.StringIds);
        Assert.Equal(2, result.InsertCount);
        Assert.Equal(0, result.DeleteCount);
        Assert.Equal(1, result.UpsertCount);
        Assert.Equal(123456789UL, result.Timestamp);
    }

    [Fact]
    public void Mutation_parses_string_ids()
    {
        var response = new Grpc.MutationResult
        {
            IDs = new Grpc.IDs { StrId = new Grpc.StringArray { Data = { "a", "b" } } }
        };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Null(result.LongIds);
        Assert.Equal(new[] { "a", "b" }, result.StringIds);
    }

    [Fact]
    public void Mutation_without_ids_returns_null_id_lists()
    {
        var response = new Grpc.MutationResult { InsertCnt = 5 };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Null(result.LongIds);
        Assert.Null(result.StringIds);
        Assert.Equal(5, result.InsertCount);
    }

    [Fact]
    public void Mutation_parses_partial_failure_indexes()
    {
        var response = new Grpc.MutationResult
        {
            InsertCnt = 2,
            SuccIndex = { 0, 2 },
            ErrIndex = { 1 }
        };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Equal(new long[] { 0, 2 }, result.SuccessIndex);
        Assert.Equal(new long[] { 1 }, result.ErrorIndex);
    }

    [Fact]
    public void Mutation_leaves_partial_failure_indexes_null_when_absent()
    {
        var response = new Grpc.MutationResult { InsertCnt = 3 };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Null(result.SuccessIndex);
        Assert.Null(result.ErrorIndex);
    }

    [Fact]
    public void Get_parses_scalar_and_vector_fields()
    {
        var response = new Grpc.QueryResults
        {
            FieldsData =
            {
                new Grpc.FieldData
                {
                    FieldName = "id",
                    Type = Grpc.DataType.Int64,
                    Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 1L, 2L } } }
                },
                new Grpc.FieldData
                {
                    FieldName = "embedding",
                    Type = Grpc.DataType.FloatVector,
                    Vectors = new Grpc.VectorField
                    {
                        Dim = 2,
                        FloatVector = new Grpc.FloatArray { Data = { 0.1f, 0.2f, 0.3f, 0.4f } }
                    }
                }
            }
        };

        GetResp result = GetResp.FromGrpc(response);

        Assert.Equal(2, result.FieldsData.Count);

        var id = Assert.IsType<FieldData<long>>(result.FieldsData[0]);
        Assert.Equal("id", id.FieldName);
        Assert.Equal(new[] { 1L, 2L }, id.Data);

        var embedding = Assert.IsType<FloatVectorFieldData>(result.FieldsData[1]);
        Assert.Equal(2, embedding.Data.Count);
        Assert.Equal(new[] { 0.1f, 0.2f }, embedding.Data[0].ToArray());
        Assert.Equal(new[] { 0.3f, 0.4f }, embedding.Data[1].ToArray());
    }

    [Fact]
    public void Get_surfaces_dynamic_fields_as_json()
    {
        var response = new Grpc.QueryResults
        {
            FieldsData =
            {
                new Grpc.FieldData
                {
                    FieldName = "id",
                    Type = Grpc.DataType.Int64,
                    Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 1L } } }
                },
                new Grpc.FieldData
                {
                    FieldName = "",
                    Type = Grpc.DataType.Json,
                    IsDynamic = true,
                    Scalars = new Grpc.ScalarField
                    {
                        JsonData = new Grpc.JSONArray { Data = { ByteString.CopyFromUtf8("{\"a\":1}") } }
                    }
                }
            }
        };

        GetResp result = GetResp.FromGrpc(response);

        Assert.Equal(2, result.FieldsData.Count);
        Assert.Equal("id", result.FieldsData[0].FieldName);
        Assert.True(result.FieldsData[1].IsDynamic);
        Assert.Equal(DataType.Json, result.FieldsData[1].DataType);
    }

    [Fact]
    public void Get_parses_json_field()
    {
        var response = new Grpc.QueryResults
        {
            FieldsData =
            {
                new Grpc.FieldData
                {
                    FieldName = "meta",
                    Type = Grpc.DataType.Json,
                    Scalars = new Grpc.ScalarField
                    {
                        JsonData = new Grpc.JSONArray
                        {
                            Data = { ByteString.CopyFromUtf8("{\"a\":1}"), ByteString.CopyFromUtf8("{\"a\":2}") }
                        }
                    }
                }
            }
        };

        GetResp result = GetResp.FromGrpc(response);

        var meta = Assert.IsType<FieldData<string>>(result.FieldsData[0]);
        Assert.Equal(DataType.Json, meta.DataType);
        Assert.Equal(new[] { "{\"a\":1}", "{\"a\":2}" }, meta.Data);
    }

    [Fact]
    public void Query_maps_collection_name_and_fields()
    {
        var response = new Grpc.QueryResults
        {
            CollectionName = "book",
            SessionTs = 12345,
            FieldsData =
            {
                new Grpc.FieldData
                {
                    FieldName = "id",
                    Type = Grpc.DataType.Int64,
                    Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 1L, 2L } } }
                },
                new Grpc.FieldData
                {
                    FieldName = "title",
                    Type = Grpc.DataType.VarChar,
                    Scalars = new Grpc.ScalarField { StringData = new Grpc.StringArray { Data = { "a", "b" } } }
                }
            }
        };

        QueryResp result = QueryResp.FromGrpc(response);

        Assert.Equal("book", result.CollectionName);
        Assert.Equal(2, result.FieldsData.Count);
        Assert.Equal(12345UL, result.SessionTs);

        var title = Assert.IsType<FieldData<string>>(result.FieldsData[1]);
        Assert.Equal("title", title.FieldName);
        Assert.Equal(new[] { "a", "b" }, title.Data);
    }

    [Fact]
    public void Search_maps_all_properties()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            SessionTs = 999,
            Results = new Grpc.SearchResultData
            {
                NumQueries = 1,
                TopK = 3,
                FieldsData =
                {
                    new Grpc.FieldData
                    {
                        FieldName = "id",
                        Type = Grpc.DataType.Int64,
                        Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 1L, 2L, 3L } } }
                    }
                },
                Scores = { 0.9f, 0.8f, 0.7f },
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L, 2L, 3L } } },
                Topks = { 3L }
            }
        };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Equal("book", result.CollectionName);
        Assert.Single(result.FieldsData);
        Assert.Equal(new[] { 0.9f, 0.8f, 0.7f }, result.Scores);
        Assert.Equal(new[] { 1L, 2L, 3L }, result.Ids.LongIds);
        Assert.Null(result.Ids.StringIds);
        Assert.Equal(1, result.NumQueries);
        Assert.Equal(3, result.Limit);
        Assert.Equal(new[] { 3L }, result.Limits);
        Assert.Equal(999UL, result.SessionTs);
    }

    [Fact]
    public void Search_maps_highlights_group_by_and_element_indices()
    {
        var response = new Grpc.SearchResults
        {
            Results = new Grpc.SearchResultData
            {
                NumQueries = 1,
                TopK = 1,
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L } } },
                Scores = { 0.5f },
                GroupByFieldValue = new Grpc.FieldData
                {
                    FieldName = "group_by_field_value",
                    Type = Grpc.DataType.Int64,
                    Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 7L } } }
                },
                HighlightResults =
                {
                    new Grpc.HighlightResult
                    {
                        FieldName = "title",
                        Datas =
                        {
                            new Grpc.HighlightData
                            {
                                Fragments = { "Milvus", "vector" },
                                Scores = { 0.9f, 0.8f }
                            }
                        }
                    }
                },
                ElementIndices = new Grpc.LongArray { Data = { 0L } }
            }
        };

        SearchResp result = SearchResp.FromGrpc(response);

        HighlightResult highlight = Assert.Single(result.HighlightResults!);
        Assert.Equal("title", highlight.FieldName);
        MilvusHighlightData data = Assert.Single(highlight.Datas);
        Assert.Equal(new[] { "Milvus", "vector" }, data.Fragments);
        Assert.Equal(new[] { 0.9f, 0.8f }, data.Scores);

        Assert.NotNull(result.GroupByFieldValue);
        Assert.Equal(new[] { 0L }, result.ElementIndices);
    }

    [Fact]
    public void Search_maps_aggregation_buckets()
    {
        var response = new Grpc.SearchResults
        {
            Results = new Grpc.SearchResultData
            {
                NumQueries = 1,
                TopK = 1,
                PrimaryFieldName = "id"
            }
        };
        var bucket = new Grpc.AggBucket { Count = 3 };
        bucket.Key.Add(new Grpc.BucketKeyEntry { FieldId = 100, FieldName = "color", StringVal = "red" });
        bucket.Metrics["avg_price"] = new Grpc.MetricValue { DoubleVal = 12.5 };
        var hit = new Grpc.AggHit { IntPk = 42, Score = 0.9f };
        hit.Fields.Add(new Grpc.AggHitField { FieldId = 200, FieldName = "title", StringVal = "book" });
        bucket.Hits.Add(hit);
        var sub = new Grpc.AggBucket { Count = 2 };
        sub.Key.Add(new Grpc.BucketKeyEntry { FieldId = 300, FieldName = "size", IntVal = 5 });
        bucket.SubGroups.Add(sub);
        response.Results.AggBuckets.Add(bucket);
        response.Results.AggTopks.Add(1);

        SearchResp result = SearchResp.FromGrpc(response);

        AggregationBucket single = Assert.Single(Assert.Single(result.AggregationBuckets));
        Assert.Equal(3, single.Count);
        AggregationBucketKey key = Assert.Single(single.Key);
        Assert.Equal(100, key.FieldId);
        Assert.Equal("color", key.FieldName);
        Assert.Equal("red", key.Value);
        Assert.Equal(12.5, Assert.IsType<double>(single.Metrics["avg_price"]));
        AggregationHit aggregateHit = Assert.Single(single.Hits);
        Assert.Equal(42L, aggregateHit.Id);
        Assert.Equal(0.9f, aggregateHit.Score);
        Assert.Equal("book", Assert.Single(aggregateHit.Fields).Value);
        AggregationBucket subBucket = Assert.Single(single.SubGroups);
        Assert.Equal(5L, Assert.Single(subBucket.Key).Value);
    }

    [Fact]
    public void Search_aggregation_buckets_empty_without_results()
    {
        SearchResp result = SearchResp.FromGrpc(new Grpc.SearchResults());

        Assert.Empty(result.AggregationBuckets);
    }

    [Fact]
    public void Search_aggregation_rejects_negative_agg_topk()
    {
        var response = new Grpc.SearchResults
        {
            Results = new Grpc.SearchResultData { NumQueries = 1, TopK = 1, PrimaryFieldName = "id" }
        };
        response.Results.AggBuckets.Add(new Grpc.AggBucket());
        response.Results.AggTopks.Add(-1);

        MilvusException ex = Assert.Throws<MilvusException>(() => SearchResp.FromGrpc(response));
        Assert.Contains("cannot be negative", ex.Message);
    }

    [Fact]
    public void Search_aggregation_rejects_bucket_count_mismatch()
    {
        var response = new Grpc.SearchResults
        {
            Results = new Grpc.SearchResultData { NumQueries = 1, TopK = 1, PrimaryFieldName = "id" }
        };
        // One query declares two buckets but only one is returned.
        response.Results.AggBuckets.Add(new Grpc.AggBucket());
        response.Results.AggTopks.Add(2);

        MilvusException ex = Assert.Throws<MilvusException>(() => SearchResp.FromGrpc(response));
        Assert.Contains("do not account for every returned bucket", ex.Message);
    }

    [Fact]
    public void Search_preserves_zero_all_search_count()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            Results = new Grpc.SearchResultData
            {
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L } } },
                AllSearchCount = 0
            }
        };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Equal(0, result.AllSearchCount);
    }

    [Fact]
    public void Search_all_search_count_is_null_only_without_results()
    {
        // AllSearchCount is an int64 proto field, so both "zero matches" and "not reported" surface as 0;
        // null is reserved for a response that carries no result data at all.
        var response = new Grpc.SearchResults { CollectionName = "book" };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Null(result.AllSearchCount);
    }

    [Fact]
    public void Search_parses_string_ids()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            Results = new Grpc.SearchResultData
            {
                Ids = new Grpc.IDs { StrId = new Grpc.StringArray { Data = { "a", "b" } } }
            }
        };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Equal(new[] { "a", "b" }, result.Ids.StringIds);
        Assert.Null(result.Ids.LongIds);
    }

    [Fact]
    public void Search_without_ids_yields_default_ids()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            Results = new Grpc.SearchResultData()
        };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Equal(default, result.Ids);
        Assert.Null(result.Ids.LongIds);
        Assert.Null(result.Ids.StringIds);
    }

    [Fact]
    public void Search_parses_metrics_from_results_and_status_extra_info()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            Results = new Grpc.SearchResultData
            {
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L } } },
                Recalls = { 0.5f, 0.75f }
            },
            Status = new Grpc.Status
            {
                ExtraInfo =
                {
                    { "report_value", "12" },
                    { "scanned_remote_bytes", "100" },
                    { "scanned_total_bytes", "200" },
                    { "cache_hit_ratio", "0.8" }
                }
            }
        };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Equal(new[] { 0.5f, 0.75f }, result.Recalls);
        Assert.Equal(12, result.Cost);
        Assert.Equal(100, result.ScannedRemoteBytes);
        Assert.Equal(200, result.ScannedTotalBytes);
        Assert.Equal(0.8f, result.CacheHitRatio);
    }

    [Fact]
    public void Search_metrics_default_when_status_reports_nothing()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            Results = new Grpc.SearchResultData
            {
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L } } }
            }
        };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Null(result.Recalls);
        Assert.Equal(0, result.Cost);
        Assert.Equal(0, result.ScannedRemoteBytes);
        Assert.Equal(0, result.ScannedTotalBytes);
        Assert.Null(result.CacheHitRatio);
    }

    [Fact]
    public void Mutation_parses_cost_from_status_extra_info()
    {
        var response = new Grpc.MutationResult
        {
            InsertCnt = 1,
            Status = new Grpc.Status { ExtraInfo = { { "report_value", "42" } } }
        };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Equal(42, result.Cost);
    }

    [Fact]
    public void Mutation_cost_defaults_to_zero_without_report_value()
    {
        var response = new Grpc.MutationResult { InsertCnt = 1 };

        MutationResp result = MutationResp.FromGrpc(response);

        Assert.Equal(0, result.Cost);
    }

    [Fact]
    public void Search_splits_flat_results_into_per_query_single_results()
    {
        var response = new Grpc.SearchResults
        {
            CollectionName = "book",
            Results = new Grpc.SearchResultData
            {
                NumQueries = 2,
                TopK = 1,
                PrimaryFieldName = "id",
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L, 2L } } },
                Scores = { 0.9f, 0.7f },
                Topks = { 1L, 1L }
            }
        };
        var idField = new Grpc.FieldData { FieldName = "id", Type = Grpc.DataType.Int64 };
        idField.Scalars = new Grpc.ScalarField();
        idField.Scalars.LongData = new Grpc.LongArray { Data = { 1L, 2L } };
        response.Results.FieldsData.Add(idField);

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Equal(2, result.SingleResults!.Count);

        SingleResult first = result.SingleResults[0];
        Assert.Equal("id", first.PrimaryKeyName);
        Assert.Equal(new[] { 0.9f }, first.Scores);
        Assert.Equal(new[] { 1L }, first.Ids.LongIds);
        Assert.Equal(1L, first.GetRow(0)["id"]);
        Assert.Equal(0.9f, first.GetRow(0)["score"]);

        SingleResult second = result.SingleResults[1];
        Assert.Equal(new[] { 0.7f }, second.Scores);
        Assert.Equal(new[] { 2L }, second.Ids.LongIds);
    }

    [Fact]
    public void Search_single_results_null_without_result_data()
    {
        var response = new Grpc.SearchResults { CollectionName = "book" };

        SearchResp result = SearchResp.FromGrpc(response);

        Assert.Null(result.SingleResults);
    }

    [Fact]
    public void SingleResult_SelectRows_keeps_only_selected_rows()
    {
        var response = new Grpc.SearchResults
        {
            Results = new Grpc.SearchResultData
            {
                NumQueries = 1,
                TopK = 3,
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L, 2L, 3L } } },
                Scores = { 0.9f, 0.8f, 0.7f },
                Topks = { 3L },
                FieldsData =
                {
                    new Grpc.FieldData
                    {
                        FieldName = "id",
                        Type = Grpc.DataType.Int64,
                        Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 1L, 2L, 3L } } }
                    }
                }
            }
        };

        SingleResult page = SearchResp.FromGrpc(response).SingleResults![0];

        SingleResult filtered = page.SelectRows(new[] { 0, 2 });

        Assert.Equal("id", filtered.PrimaryKeyName);
        Assert.Equal(new[] { 0.9f, 0.7f }, filtered.Scores);
        Assert.Equal(new[] { 1L, 3L }, filtered.Ids.LongIds);
        Assert.Equal(1L, filtered.GetRow(0)["id"]);
        Assert.Equal(3L, filtered.GetRow(1)["id"]);

        // An empty selection yields an empty page with no output fields.
        SingleResult empty = page.SelectRows(Array.Empty<int>());
        Assert.Empty(empty.Scores);
        Assert.Empty(empty.OutputFields);
    }

    [Fact]
    public void SingleResult_Merge_concatenates_pages()
    {
        var response = new Grpc.SearchResults
        {
            Results = new Grpc.SearchResultData
            {
                NumQueries = 1,
                TopK = 2,
                Ids = new Grpc.IDs { IntId = new Grpc.LongArray { Data = { 1L, 2L } } },
                Scores = { 0.9f, 0.8f },
                Topks = { 2L },
                FieldsData =
                {
                    new Grpc.FieldData
                    {
                        FieldName = "id",
                        Type = Grpc.DataType.Int64,
                        Scalars = new Grpc.ScalarField { LongData = new Grpc.LongArray { Data = { 1L, 2L } } }
                    }
                }
            }
        };

        SingleResult first = SearchResp.FromGrpc(response).SingleResults![0];
        SingleResult second = first.SelectRows(new[] { 1 });

        SingleResult merged = SingleResult.Merge(new[] { first, second });

        Assert.Equal("id", merged.PrimaryKeyName);
        Assert.Equal(new[] { 0.9f, 0.8f, 0.8f }, merged.Scores);
        Assert.Equal(new[] { 1L, 2L, 2L }, merged.Ids.LongIds);
        Assert.Equal(3, merged.OutputFields[0].RowCount);

        // A single page merges to itself.
        Assert.Same(first, SingleResult.Merge(new[] { first }));
    }

    [Fact]
    public void FieldData_SelectRows_and_Concat_preserve_concrete_vector_type()
    {
        var page = new SingleResult(
            "id",
            Milvus.Client.V2.Types.Ids.FromSlices(new[] { 1L, 2L, 3L }, null),
            new[] { 0.9f, 0.8f, 0.7f },
            new[] { FieldData.CreateFloatVector("vector", new[]
            {
                new ReadOnlyMemory<float>(new[] { 1f, 2f }),
                new ReadOnlyMemory<float>(new[] { 3f, 4f }),
                new ReadOnlyMemory<float>(new[] { 5f, 6f })
            }) });

        FieldData selected = page.OutputFields[0].SelectRows(new[] { 0, 2 });
        Assert.IsType<FloatVectorFieldData>(selected);
        Assert.Equal(2, selected.RowCount);
        Assert.Equal(new[] { 1f, 2f }, ((FloatVectorFieldData)selected).Data[0].ToArray());
        Assert.Equal(new[] { 5f, 6f }, ((FloatVectorFieldData)selected).Data[1].ToArray());

        FieldData concat = page.OutputFields[0].Concat(selected);
        Assert.IsType<FloatVectorFieldData>(concat);
        Assert.Equal(5, concat.RowCount);
        Assert.Equal(new[] { 5f, 6f }, ((FloatVectorFieldData)concat).Data[4].ToArray());
    }
}
