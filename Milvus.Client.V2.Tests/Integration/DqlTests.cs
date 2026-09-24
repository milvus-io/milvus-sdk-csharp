using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class DqlTests
{
    [Fact]
    public async Task Search_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SearchResp response = await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 2
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastSearchedCollection);
        Assert.Equal(2, server.Service.LastSearchTopK);
        Assert.Equal(2, response.Ids.LongIds!.Count);
        Assert.Equal(2, response.Scores.Count);
        Assert.Equal(0.5f, response.Scores[0]);
    }

    [Fact]
    public async Task Search_forwards_request_level_database_name()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                DatabaseName = "other_db",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 2
            },
            TestContext.Current.CancellationToken);

        Grpc.SearchRequest request = Assert.IsType<Grpc.SearchRequest>(server.Service.Requests["Search"]);
        Assert.Equal("other_db", request.DbName);
        Assert.Equal("coll", request.CollectionName);
    }

    [Fact]
    public async Task Search_uses_ts_cache_for_session_consistency()
    {
        using var server = new MockMilvusServer { Service = { NextMutationTimestamp = 100 } };
        using MilvusClientV2 client = server.CreateClient();

        // Simulate a prior DML so the ts cache is populated.
        CollectionTsCache.Instance.Clear();
        CollectionTsCache.Instance.Set(server.Uri, "default", "session_coll", 100);

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "session_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = new SearchParameters { ConsistencyLevel = ConsistencyLevel.Session }
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(100UL, server.Service.LastSearchGuaranteeTimestamp);
    }

    [Fact]
    public async Task Search_unset_consistency_still_sends_session_guarantee_timestamp()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        CollectionTsCache.Instance.Clear();
        CollectionTsCache.Instance.Set(server.Uri, "default", "session_coll", 200);

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "session_coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1
            },
            TestContext.Current.CancellationToken);

        // Unset consistency still carries the Session-style cached timestamp (matching C++ DeduceGuaranteeTimestamp
        // on NONE and the design doc §4.6), so insert-then-search honors read-your-writes.
        Assert.Equal(200UL, server.Service.LastSearchGuaranteeTimestamp);
        Assert.True(server.Service.LastSearchUseDefaultConsistency);
    }

    [Fact]
    public async Task Search_serializes_ignore_growing()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters { IgnoreGrowing = true };

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("true", byKey["ignore_growing"]);
    }

    [Fact]
    public async Task Search_serializes_timezone_radius_range_filter_and_rerank()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = new SearchParameters
                {
                    Timezone = "+08:00",
                    Radius = "0.5",
                    RangeFilter = "1.0"
                }
            },
            TestContext.Current.CancellationToken);

        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("+08:00", byKey["timezone"]);
        Assert.Equal("0.5", byKey["radius"]);
        Assert.Equal("1.0", byKey["range_filter"]);

        // The proxy detects range search from the "params" JSON string, so radius/range_filter must also be
        // embedded there (as numbers, since string-typed radius/range_filter is rejected server-side).
        using var paramsJson = System.Text.Json.JsonDocument.Parse(byKey["params"]);
        Assert.Equal(0.5, paramsJson.RootElement.GetProperty("radius").GetDouble());
        Assert.Equal(1.0, paramsJson.RootElement.GetProperty("range_filter").GetDouble());
    }

    [Fact]
    public async Task Search_serializes_group_by_parameters()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = new SearchParameters
                {
                    GroupByField = "category",
                    GroupSize = 5,
                    StrictGroupSize = true
                }
            },
            TestContext.Current.CancellationToken);

        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("category", byKey["group_by_field"]);
        Assert.Equal("5", byKey["group_size"]);
        Assert.Equal("true", byKey["strict_group_size"]);
    }

    [Fact]
    public async Task HybridSearch_serializes_group_by_on_sub_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                Limit = 1,
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                        MetricType = SimilarityMetricType.L2,
                        Limit = 1,
                        GroupByField = "category",
                        GroupSize = 3
                    }
                ]
            },
            TestContext.Current.CancellationToken);

        var search = Assert.IsType<Milvus.Client.Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);
        var subByKey = search.Requests[0].SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("category", subByKey["group_by_field"]);
        Assert.Equal("3", subByKey["group_size"]);
    }

    [Fact]
    public async Task Search_rejects_Rerank_as_having_no_wire_representation()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters { Rerank = "rrf" };

        await Assert.ThrowsAsync<ArgumentException>(async () => await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Search_by_ids_sends_ids_instead_of_placeholder()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Ids = new object[] { 1L, 2L, 3L },
                MetricType = SimilarityMetricType.L2,
                Limit = 3
            },
            TestContext.Current.CancellationToken);

        var request = Assert.IsType<Milvus.Client.Grpc.SearchRequest>(server.Service.Requests["Search"]);
        Assert.NotNull(request.Ids);
        Assert.Equal(new long[] { 1L, 2L, 3L }, request.Ids.IntId.Data);
        Assert.True(string.IsNullOrEmpty(request.PlaceholderGroup.ToStringUtf8()) || request.PlaceholderGroup.Length == 0);
    }

    [Theory]
    [InlineData((sbyte)1)]
    [InlineData((ushort)1)]
    [InlineData(1UL)]
    public async Task Search_by_ids_accepts_full_integral_widths(object first)
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Ids = new object[] { first, Convert.ChangeType(first, first.GetType(), System.Globalization.CultureInfo.InvariantCulture)! },
                MetricType = SimilarityMetricType.L2,
                Limit = 2
            },
            TestContext.Current.CancellationToken);

        var request = Assert.IsType<Milvus.Client.Grpc.SearchRequest>(server.Service.Requests["Search"]);
        Assert.NotNull(request.Ids);
        Assert.Equal(new long[] { 1L, 1L }, request.Ids.IntId.Data);
    }

    [Fact]
    public async Task Search_rejects_offset_plus_limit_out_of_range()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var request = new SearchReq
        {
            CollectionName = "coll",
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 2,
            Parameters = new SearchParameters { Offset = 20000 }
        };

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.SearchAsync(request, TestContext.Current.CancellationToken));
        Assert.Contains("sum of Limit and Offset", ex.Message);
    }

    [Fact]
    public async Task Query_forwards_expression()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        QueryResp response = await client.QueryAsync(
            new QueryReq
            {
                CollectionName = "coll",
                Expression = "id in [1, 2]",
                Parameters = new QueryParameters { OutputFields = { "id" } }
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("coll", server.Service.LastQueriedCollection);
        Assert.Equal("id in [1, 2]", server.Service.LastQueryExpression);
        Assert.Single(response.FieldsData);
    }

    [Fact]
    public async Task Query_uses_ts_cache_for_session_consistency()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        CollectionTsCache.Instance.Clear();
        CollectionTsCache.Instance.Set(server.Uri, "default", "session_coll", 300);

        await client.QueryAsync(
            new QueryReq
            {
                CollectionName = "session_coll",
                Expression = "id in [1, 2]",
                Parameters = new QueryParameters { ConsistencyLevel = ConsistencyLevel.Session }
            },
            TestContext.Current.CancellationToken);

        // The Query path must stamp the Session guarantee timestamp for read-your-writes, like Search.
        Assert.Equal(300UL, server.Service.LastQueryGuaranteeTimestamp);
    }

    [Fact]
    public async Task Get_uses_ts_cache_for_session_consistency()
    {
        using var server = new MockMilvusServer { Service = { DescribeSchema = BuildSchema() } };
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        CollectionTsCache.Instance.Clear();
        CollectionTsCache.Instance.Set(server.Uri, "default", "coll", 400);

        await client.GetAsync(
            new GetReq { CollectionName = "coll", Ids = new object[] { 1L, 2L } },
            TestContext.Current.CancellationToken);

        // The Get path must stamp the Session guarantee timestamp for read-your-writes, like Query/Search.
        Assert.Equal(400UL, server.Service.LastQueryGuaranteeTimestamp);
    }

    [Fact]
    public async Task Get_builds_query_by_primary_key()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // The mock DescribeCollection has no schema, so Get needs one via the (mock) describe path.
        // Give the mock a describable schema by adding a primary-key field through DescribeCollectionResp.
        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildSchema();

        GetResp response = await client.GetAsync(
            new GetReq { CollectionName = "coll", Ids = new object[] { 1L, 2L } },
            TestContext.Current.CancellationToken);

        Assert.Single(response.FieldsData);
        Assert.Equal("id in [1, 2]", server.Service.LastQueryExpression);
    }

    [Fact]
    public async Task Search_forwards_highlighter_type()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters
        {
            HighlightType = HighlightType.Semantic
        };
        parameters.Highlighter["max_length"] = "20";

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        var grpcRequest = Assert.IsType<Milvus.Client.Grpc.SearchRequest>(server.Service.Requests["Search"]);
        Assert.Equal(Grpc.HighlightType.Semantic, grpcRequest.Highlighter.Type);
        Assert.Equal("20", grpcRequest.Highlighter.Params.Single(p => p.Key == "max_length").Value);
    }

    [Fact]
    public async Task Search_parses_metrics_from_status_extra_info()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        server.Service.SearchMetricsStatus = new Milvus.Client.Grpc.Status
        {
            ExtraInfo =
            {
                { "report_value", "7" },
                { "scanned_remote_bytes", "30" },
                { "scanned_total_bytes", "60" },
                { "cache_hit_ratio", "0.9" }
            }
        };

        SearchResp response = await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 1
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(7, response.Cost);
        Assert.Equal(30, response.ScannedRemoteBytes);
        Assert.Equal(60, response.ScannedTotalBytes);
        Assert.Equal(0.9f, response.CacheHitRatio);
    }

    [Fact]
    public async Task HybridSearch_forwards_sub_requests_and_reranker()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SearchResp response = await client.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                        MetricType = SimilarityMetricType.L2,
                        Limit = 10
                    },
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                        MetricType = SimilarityMetricType.Ip,
                        Limit = 10
                    }
                ],
                Limit = 3
            },
            TestContext.Current.CancellationToken);

        var grpcRequest =
            Assert.IsType<Milvus.Client.Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);
        Assert.Equal("coll", grpcRequest.CollectionName);
        Assert.Equal(2, grpcRequest.Requests.Count);
        Assert.Equal(2, grpcRequest.Requests.Count(r => r.PlaceholderGroup.Length > 0));

        var rankParams = grpcRequest.RankParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("3", rankParams["limit"]);
        Assert.Equal("rrf", rankParams["strategy"]);

        Assert.Equal(3, response.Ids.LongIds!.Count);
        Assert.Equal(3, response.Scores.Count);
    }

    [Fact]
    public async Task HybridSearch_forwards_embedding_list_sub_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "clips[vector]",
                        EmbeddingLists =
                        [
                            new EmbeddingList(new[]
                            {
                                new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }),
                                new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f })
                            })
                        ],
                        MetricType = SimilarityMetricType.MaxSimCosine,
                        Limit = 5
                    }
                ],
                Limit = 5
            },
            TestContext.Current.CancellationToken);

        var grpcRequest =
            Assert.IsType<Milvus.Client.Grpc.HybridSearchRequest>(server.Service.Requests["HybridSearch"]);
        Grpc.SearchRequest sub = Assert.Single(grpcRequest.Requests);
        Assert.Equal("clips[vector]", sub.SearchParams.Single(p => p.Key == "anns_field").Value);
        Assert.Equal("MAX_SIM_COSINE", sub.SearchParams.Single(p => p.Key == "metric_type").Value);
        // nq is the embedding-list count (the struct elements to score), and the placeholder type is EmbListFloatVector.
        Assert.Equal(1, sub.Nq);
        Grpc.PlaceholderGroup group = Grpc.PlaceholderGroup.Parser.ParseFrom(sub.PlaceholderGroup);
        Grpc.PlaceholderValue value = Assert.Single(group.Placeholders);
        Assert.Equal(Grpc.PlaceholderType.EmbListFloatVector, value.Type);
    }

    [Fact]
    public async Task QueryIterator_pages_over_results_with_pk_cursor()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        await client.InsertAsync(
            new InsertReq
            {
                CollectionName = "iter_coll",
                ColumnsData =
                [
                    FieldData.Create("id", new long[] { 1L }),
                    FieldData.CreateFloatVector("embedding", new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) })
                ]
            },
            TestContext.Current.CancellationToken);

        // A Limit of 1 bounds the cursor-driven loop to a single page against the mock.
        int batches = 0;
        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = new QueryParameters { Limit = 1 }
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            batches++;
            Assert.NotNull(batch);
        }

        Assert.Equal(1, batches);
        Assert.True(server.Service.Requests.ContainsKey("DescribeCollection"));
        Assert.True(server.Service.Requests.ContainsKey("Query"));

        var grpcRequest = Assert.IsType<Milvus.Client.Grpc.QueryRequest>(server.Service.Requests["Query"]);
        Assert.Equal("iter_coll", grpcRequest.CollectionName);
        Assert.Equal("", grpcRequest.Expr);   // no user expression -> full-range first page, no min-bound literal
        // With an empty output-field list the server returns all fields (including the pk), so the
        // iterator does not add the pk to OutputFields explicitly.
        Assert.Empty(grpcRequest.OutputFields);

        var queryParams = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("True", queryParams["iterator"]);
        Assert.Equal("1", queryParams["batch_size"]);
        Assert.Equal("1", queryParams["limit"]);
    }

    [Fact]
    public async Task QueryIterator_pages_with_pk_cursor_advancement()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 5;
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();

        var expressions = new List<string>();
        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 2
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.Single(batch);
            expressions.Add(server.Service.LastQueryExpression!);
        }

        // 5 rows at batch size 2: three non-empty pages plus a final empty page that ends the iteration.
        Assert.Equal(4, server.Service.LastQueryCount);
        Assert.Equal("", expressions[0]);
        Assert.StartsWith("id > 1", expressions[1]);
        Assert.StartsWith("id > 3", expressions[2]);
        Assert.Equal(new[] { "2", "2", "2", "2" }, server.Service.LastQueryLimit);
    }

    [Fact]
    public async Task QueryIterator_advances_varchar_pk_cursor_with_escaped_literal()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = BuildIteratorStringSchema();
        server.Service.QueryIteratorStringPkRows = 5;
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();

        var expressions = new List<string>();
        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 2
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.Single(batch);
            expressions.Add(server.Service.LastQueryExpression!);
        }

        // 5 VarChar pks at batch size 2: three non-empty pages plus a final empty page that ends the iteration.
        Assert.Equal(4, server.Service.LastQueryCount);
        Assert.Equal("", expressions[0]);
        Assert.Equal("id > 'pk_0002'", expressions[1]);
        Assert.Equal("id > 'pk_0004'", expressions[2]);
        Assert.Equal(new[] { "2", "2", "2", "2" }, server.Service.LastQueryLimit);
    }

    [Fact]
    public async Task QueryIterator_forwards_ignore_growing_timezone_and_filter_templates()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        var parameters = new QueryParameters
        {
            Limit = 1,
            IgnoreGrowing = true,
            Timezone = "+08:00"
        };
        parameters.FilterTemplates["status"] = "active";

        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                Expression = "status == @status",
                BatchSize = 1000,
                Parameters = parameters
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            Assert.NotNull(batch);
        }

        var grpcRequest = Assert.IsType<Milvus.Client.Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var queryParams = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("true", queryParams["ignore_growing"]);
        Assert.Equal("+08:00", queryParams["timezone"]);
        Assert.True(grpcRequest.ExprTemplateValues.ContainsKey("status"));
        Assert.Equal("active", grpcRequest.ExprTemplateValues["status"].StringVal);
    }

    [Fact]
    public async Task QueryIterator_rejects_ids_parameter()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (IReadOnlyList<FieldData> batch in client
                .QueryIteratorAsync(new QueryIteratorReq
                {
                    CollectionName = "iter_coll",
                    BatchSize = 1000,
                    Parameters = new QueryParameters { Ids = new object[] { 1L } }
                })
                .WithCancellation(TestContext.Current.CancellationToken))
            {
                Assert.NotNull(batch);
            }
        });
    }

    [Fact]
    public async Task SearchIterator_issues_search_with_iterator_params()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        // The mock cannot produce a search_iter_v2 token, so enumeration throws after issuing the
        // DescribeCollection + Search RPCs; assert those RPCs and their iterator parameters instead.
        await Assert.ThrowsAsync<MilvusException>(async () =>
        {
            await foreach (SingleResult page in client
                .SearchIteratorAsync(new SearchIteratorReq
                {
                    CollectionName = "iter_coll",
                    VectorFieldName = "embedding",
                    Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                    MetricType = SimilarityMetricType.L2,
                    Limit = 1,
                    BatchSize = 10
                })
                .WithCancellation(TestContext.Current.CancellationToken))
            {
                Assert.NotNull(page);
            }
        });

        Assert.True(server.Service.Requests.ContainsKey("DescribeCollection"));
        Assert.True(server.Service.Requests.ContainsKey("Search"));

        var grpcRequest = Assert.IsType<Milvus.Client.Grpc.SearchRequest>(server.Service.Requests["Search"]);
        Assert.Equal("iter_coll", grpcRequest.CollectionName);
        Assert.Equal("embedding", grpcRequest.SearchParams.Single(p => p.Key == "anns_field").Value);

        var searchParams = grpcRequest.SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("True", searchParams["iterator"]);
        Assert.Equal("True", searchParams["search_iter_v2"]);
        Assert.Equal("1", searchParams["topk"]);
        Assert.Equal("1", searchParams["search_iter_batch_size"]);
    }

    [Fact]
    public async Task QueryIterator_caps_over_delivered_pages_to_the_limit()
    {
        using var server = new MockMilvusServer();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 10;
        // Simulate reduce_stop_for_best over-delivery: the mock returns 5 extra rows beyond the requested limit.
        server.Service.QueryOverDeliver = 5;
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();

        var rows = new List<long>();
        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 2,
                Parameters = new QueryParameters { Limit = 3 }
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            var idColumn = (FieldData<long>)batch.Single(f => f.FieldName == "id");
            rows.AddRange(idColumn.Data);
        }

        // Even though page 1 over-delivers 7 rows, the iterator yields only the 3 the user asked for.
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public async Task SearchIterator_success_path_pins_token_and_iterates()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        // The mock returns a search-iterator token on every page; the loop ends via the remaining count.
        server.Service.SearchIteratorToken = "tok-1";
        // The server pins a snapshot timestamp for the iterator, which the client must forward on page 2.
        server.Service.NextSearchSessionTs = 999;

        var pages = new List<SingleResult>();
        await foreach (SingleResult page in client
            .SearchIteratorAsync(new SearchIteratorReq
            {
                CollectionName = "iter_coll_success",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = SimilarityMetricType.L2,
                Limit = 2,
                BatchSize = 1
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            pages.Add(page);
        }

        // The mock returns 1 hit per call; Limit=2 + BatchSize=1 drives two pages, then the loop ends.
        Assert.Equal(2, pages.Count);
        // Each page exposes the hit's score and primary key (the mock returns one hit with score 0.5 and id 1).
        Assert.Equal(new[] { 0.5f }, pages[0].Scores);
        Assert.Equal(1L, pages[0].Ids.LongIds![0]);
        Assert.True(server.Service.Requests.ContainsKey("DescribeCollection"));
        Assert.True(server.Service.Requests.ContainsKey("Search"));
        Assert.Equal(2, server.Service.SearchRequests.Count);

        // Page 1 carries no cursor; page 2 must forward the token and last bound returned by page 1.
        var page1 = server.Service.SearchRequests[0];
        var page1Params = page1.SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("True", page1Params["iterator"]);
        Assert.Equal("True", page1Params["search_iter_v2"]);
        Assert.Equal("1", page1Params["topk"]);
        Assert.Equal("1", page1Params["search_iter_batch_size"]);
        Assert.DoesNotContain("search_iter_id", page1Params);

        var page2 = server.Service.SearchRequests[1];
        var page2Params = page2.SearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("tok-1", page2Params["search_iter_id"]);
        Assert.Equal("1.500000000000000", page2Params["search_iter_last_bound"]);
        // The snapshot timestamp returned by page 1 is pinned for page 2.
        Assert.Equal(999UL, page2.GuaranteeTimestamp);
    }

    [Fact]
    public async Task QueryIterator_seeks_past_offset_with_plain_queries()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        // 10 rows; the iterator seeks past offset 5 with a plain (iterator=False) query, then pages from id=5.
        server.Service.QueryIteratorTotalRows = 10;

        int batches = 0;
        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = new QueryParameters { Offset = 5, Limit = 2 }
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            batches++;
            Assert.NotNull(batch);
        }

        // Offset 5 + Limit 2: the seek consumes rows 0-4 with a plain query, then the single iterator page
        // (rows 5-6) satisfies the limit, so one batch is yielded.
        Assert.Equal(1, batches);

        // A seek query plus one iterator page.
        Assert.Equal(2, server.Service.LastQueryCount);
    }

    [Fact]
    public async Task QueryIterator_pins_server_session_ts_when_returned()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        // A non-zero SessionTs lets the iterator pin the server-reported snapshot instead of a client-side one.
        server.Service.NextQuerySessionTs = 100;
        // Limit exceeds the row count so a second page (carrying the pinned timestamp) is actually issued.
        server.Service.QueryIteratorTotalRows = 3;

        int batches = 0;
        await foreach (IReadOnlyList<FieldData> batch in client
            .QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = "iter_coll",
                BatchSize = 1000,
                Parameters = new QueryParameters { Limit = 5 }
            })
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            batches++;
            Assert.NotNull(batch);
        }

        Assert.Equal(1, batches);

        // Page 1 reported SessionTs 100 and the iterator pinned it; page 2 (the only subsequent Query) must
        // carry GuaranteeTimestamp == 100, not the client-side fallback timestamp.
        var grpcRequest = Assert.IsType<Milvus.Client.Grpc.QueryRequest>(server.Service.Requests["Query"]);
        Assert.Equal(100UL, grpcRequest.GuaranteeTimestamp);
    }

    private static CollectionSchema BuildIteratorSchema()
    {
        var schema = new CollectionSchema { Name = "iter_coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateFloatVector("embedding", 4));
        return schema;
    }

    private static CollectionSchema BuildIteratorStringSchema()
    {
        var schema = new CollectionSchema { Name = "iter_coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.VarChar, isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateFloatVector("embedding", 4));
        return schema;
    }

    private static CollectionSchema BuildSchema()
    {
        var schema = new CollectionSchema { Name = "coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        return schema;
    }
}
