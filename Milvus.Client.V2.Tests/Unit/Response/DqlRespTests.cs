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

        MilvusHighlightResult highlight = Assert.Single(result.HighlightResults!);
        Assert.Equal("title", highlight.FieldName);
        MilvusHighlightData data = Assert.Single(highlight.Datas);
        Assert.Equal(new[] { "Milvus", "vector" }, data.Fragments);
        Assert.Equal(new[] { 0.9f, 0.8f }, data.Scores);

        Assert.NotNull(result.GroupByFieldValue);
        Assert.Equal(new[] { 0L }, result.ElementIndices);
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
}
