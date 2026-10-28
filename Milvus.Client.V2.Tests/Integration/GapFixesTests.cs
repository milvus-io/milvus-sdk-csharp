using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Rbac;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class GapFixesTests
{
    [Fact]
    public async Task Search_forwards_typed_order_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.OrderByFields.Add(new OrderByField("title", AggregationDirection.Desc));
        parameters.OrderByFields.Add(new OrderByField("pages"));

        await client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken);

        var byKey = server.Service.LastSearchParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("title:desc,pages:asc", byKey["order_by_fields"]);
    }

    [Fact]
    public async Task Query_forwards_typed_order_and_group_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new QueryParameters();
        parameters.OrderByFields.Add(new OrderByField("title"));
        parameters.GroupByFields.Add("author");

        await client.QueryAsync(
            new QueryReq { CollectionName = "coll", Expression = "id > 0", Parameters = parameters },
            TestContext.Current.CancellationToken);

        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("title:asc", byKey["order_by_fields"]);
        Assert.Equal("author", byKey["group_by_fields"]);
    }

    [Fact]
    public async Task Search_rejects_dual_order_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.OrderByFields.Add(new OrderByField("title"));
        parameters.ExtraParameters["order_by_fields"] = "pages:desc";

        // The proxy's parseOrderByFields keeps the first match, so a dual supply would silently ignore the
        // typed list; reject it up front like the other typed index params.
        await Assert.ThrowsAsync<ArgumentException>(() => client.SearchAsync(
            new SearchReq
            {
                CollectionName = "coll",
                VectorFieldName = "embedding",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                MetricType = MetricType.L2,
                Limit = 1,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Query_typed_order_by_yields_to_extra_parameters()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new QueryParameters();
        parameters.OrderByFields.Add(new OrderByField("title"));
        parameters.GroupByFields.Add("author");
        parameters.ExtraParameters["order_by_fields"] = "pages:desc";
        parameters.ExtraParameters["group_by_fields"] = "year";

        await client.QueryAsync(
            new QueryReq { CollectionName = "coll", Expression = "id > 0", Parameters = parameters },
            TestContext.Current.CancellationToken);

        // The verbatim ExtraParameters values win (the proxy keeps the first match) and the typed emission is
        // skipped, so exactly one order_by_fields / group_by_fields reaches the wire.
        var grpcRequest = Assert.IsType<Grpc.QueryRequest>(server.Service.Requests["Query"]);
        var byKey = grpcRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("pages:desc", byKey["order_by_fields"]);
        Assert.Equal("year", byKey["group_by_fields"]);
        Assert.Single(grpcRequest.QueryParams, p => p.Key == "order_by_fields");
    }

    [Fact]
    public async Task HybridSearch_rejects_order_by_fields_on_parameters()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.OrderByFields.Add(new OrderByField("title"));

        // The hybrid request has no top-level order_by wire slot; reject it up front instead of silently
        // dropping it.
        await Assert.ThrowsAsync<ArgumentException>(() => client.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                        MetricType = MetricType.L2,
                        Limit = 10
                    }
                ],
                Limit = 3,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task QueryIterator_captures_and_resumes_cursor()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 5;
        server.Service.NextQuerySessionTs = 100;

        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1,
            Parameters = new QueryParameters { Limit = 3 }
        });

        Assert.Null(iterator.GetCursor());

        int pages = 0;
        await foreach (IReadOnlyList<FieldData> batch in iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
            pages++;
            Assert.NotNull(batch);
        }

        Assert.Equal(3, pages);

        QueryIteratorCursor? cursor = iterator.GetCursor();
        Assert.NotNull(cursor);
        Assert.Equal(2, cursor!.IntPk);
        // The iterator pins the first-page snapshot (100) for the whole iteration.
        Assert.Equal(100UL, cursor.SessionTs);

        // Resume from the captured position: rows pk 3 and 4 come in a fresh iterator.
        QueryIterator resumed = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1,
            Parameters = new QueryParameters { Limit = 2 },
            Cursor = cursor
        });

        int resumedPages = 0;
        await foreach (IReadOnlyList<FieldData> batch in resumed.WithCancellation(TestContext.Current.CancellationToken))
        {
            resumedPages++;
            Assert.NotNull(batch);
        }

        Assert.Equal(2, resumedPages);

        // The resumed iterator's pages continue from the captured key on the captured snapshot (both the
        // first and second resumed pages pin the cursor's SessionTs, and the offset seek is skipped).
        Grpc.QueryRequest firstResumed = server.Service.QueryRequests[3];
        Assert.Equal("id > 2", firstResumed.Expr);
        Assert.Equal(cursor.SessionTs, firstResumed.GuaranteeTimestamp);
        Grpc.QueryRequest secondResumed = server.Service.QueryRequests[4];
        Assert.Equal("id > 3", secondResumed.Expr);
        Assert.Equal(cursor.SessionTs, secondResumed.GuaranteeTimestamp);
        Assert.Equal(5, server.Service.QueryRequests.Count);
    }

    [Fact]
    public async Task QueryIterator_resume_rejects_mismatched_pk_type()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();

        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 10,
            // The collection's pk is Int64, but this cursor carries a string key.
            Cursor = new QueryIteratorCursor(1UL, strPk: "pk_1")
        });

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (IReadOnlyList<FieldData> batch in iterator.WithCancellation(TestContext.Current.CancellationToken))
            {
                Assert.NotNull(batch);
            }
        });
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void QueryIterator_rejects_order_by_and_group_by_fields(bool orderBy, bool groupBy)
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new QueryParameters();
        if (orderBy)
        {
            parameters.OrderByFields.Add(new OrderByField("title"));
        }

        if (groupBy)
        {
            parameters.GroupByFields.Add("author");
        }

        // pk-cursor paging requires primary-key-ordered, non-grouped output, so the iterator rejects these
        // up front instead of emitting them on every page.
        Assert.Throws<ArgumentException>(() => client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            Parameters = parameters
        }));
    }

    [Fact]
    public void QueryIterator_rejects_extra_parameters_order_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new QueryParameters();
        parameters.ExtraParameters["order_by_fields"] = "title:desc";

        // The raw ExtraParameters escape hatch is forwarded verbatim onto every iterator page, so it must be
        // rejected too; otherwise it would silently break the pk-cursor paging the typed guard prevents.
        Assert.Throws<ArgumentException>(() => client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            Parameters = parameters
        }));
    }

    [Fact]
    public void SearchIterator_rejects_order_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.OrderByFields.Add(new OrderByField("title"));

        // The search_iter_v2 token protocol pages independently of ordering.
        Assert.Throws<ArgumentException>(() => client.SearchIteratorAsync(new SearchIteratorReq
        {
            CollectionName = "iter_coll",
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
            MetricType = MetricType.L2,
            Limit = 1,
            Parameters = parameters
        }));
    }

    [Fact]
    public void SearchIterator_rejects_extra_parameters_order_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.ExtraParameters["order_by_fields"] = "title:desc";

        // The raw ExtraParameters escape hatch would be forwarded onto every search page; reject it so it
        // cannot bypass the typed guard.
        Assert.Throws<ArgumentException>(() => client.SearchIteratorAsync(new SearchIteratorReq
        {
            CollectionName = "iter_coll",
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
            MetricType = MetricType.L2,
            Limit = 1,
            Parameters = parameters
        }));
    }

    [Fact]
    public async Task QueryIterator_expands_element_filter_results()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.NextQuerySessionTs = 100;
        // Three entities (pks 0,1,2) with 2 elements each, for a total of 6 matched elements.
        server.Service.QueryElementIndices = new[] { new long[] { 0, 1 }, new long[] { 2, 3 }, new long[] { 4, 5 } };

        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1000,
            Expression = "element_filter(ArrayContains('clips', 'video'))",
            Parameters = new QueryParameters { Limit = 4 }
        });

        var pages = new List<IReadOnlyList<FieldData>>();
        await foreach (IReadOnlyList<FieldData> page in iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
            pages.Add(page);
        }

        // Limit 4 caps the 6 matched elements to one page of 4 expanded rows.
        Assert.Single(pages);
        IReadOnlyList<FieldData> firstPage = pages[0];
        Assert.Equal(4, firstPage[0].RowCount);

        // The element-filter page expands entities into one row per matched element and appends the
        // element-offset column: id values repeat per entity ([0,0,1,1]) and offsets flatten ([0,1,2,3]).
        var idData = Assert.IsType<FieldData<long>>(firstPage.Single(f => f.FieldName == "id"));
        Assert.Equal(new long[] { 0, 0, 1, 1 }, idData.Data);
        var offsetData = Assert.IsType<FieldData<long>>(firstPage.Single(f => f.FieldName == "offset"));
        Assert.Equal(new long[] { 0, 1, 2, 3 }, offsetData.Data);

        // The captured cursor carries the last yielded element offset (3) and its entity pk (1).
        QueryIteratorCursor? cursor = iterator.GetCursor();
        Assert.NotNull(cursor);
        Assert.Equal(1L, cursor!.IntPk);
        Assert.Equal(3L, cursor.LastElementOffset);
    }

    [Fact]
    public async Task QueryIterator_element_filter_resume_sends_element_params()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.NextQuerySessionTs = 100;
        server.Service.QueryElementIndices = new[] { new long[] { 0, 1 }, new long[] { 2, 3 }, new long[] { 4, 5 } };

        string expression = "element_filter(ArrayContains('clips', 'video'))";

        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1000,
            Expression = expression,
            Parameters = new QueryParameters { Limit = 4 }
        });
        await foreach (IReadOnlyList<FieldData> _ in iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
        }

        QueryIteratorCursor? cursor = iterator.GetCursor();

        QueryIterator resumed = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1000,
            Expression = expression,
            Parameters = new QueryParameters { Limit = 2 },
            Cursor = cursor
        });

        int resumedPages = 0;
        await foreach (IReadOnlyList<FieldData> _ in resumed.WithCancellation(TestContext.Current.CancellationToken))
        {
            resumedPages++;
        }

        Assert.Equal(1, resumedPages);
        Assert.Equal(2, server.Service.QueryRequests.Count);

        // The resumed page resumes inclusively (`id >= 1`) and carries the element resume params so the
        // server continues from element offset 3 of entity 1 instead of re-reading the whole entity.
        Grpc.QueryRequest resumedRequest = server.Service.QueryRequests[1];
        Assert.Contains("id >= 1", resumedRequest.Expr);
        var byKey = resumedRequest.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("1", byKey["query_iter_last_pk"]);
        Assert.Equal("3", byKey["query_iter_last_element_offset"]);
    }

    [Fact]
    public async Task GetPersistentSegmentInfo_forwards_states_filter()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.GetPersistentSegmentInfoAsync(
            new GetPersistentSegmentInfoReq
            {
                CollectionName = "coll",
                States = new[] { SegmentState.Sealed, SegmentState.Flushed }
            },
            TestContext.Current.CancellationToken);

        var request =
            Assert.IsType<Grpc.GetPersistentSegmentInfoRequest>(server.Service.Requests["GetPersistentSegmentInfo"]);
        Assert.Equal(new[] { Grpc.SegmentState.Sealed, Grpc.SegmentState.Flushed }, request.States);
    }

    [Fact]
    public async Task FlushAll_default_waits_for_completion()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // The default WaitFlushedMs of 0 means wait until the flush-all completes (no timeout), so
        // FlushAllAsync polls GetFlushAllState even without an explicit wait window.
        await client.FlushAllAsync(new FlushAllReq(), TestContext.Current.CancellationToken);

        Assert.Contains("FlushAll", server.Service.Requests.Keys);
        Assert.Contains("GetFlushAllState", server.Service.Requests.Keys);
    }

    [Fact]
    public async Task UpdatePassword_reset_connection_reconnects()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        // First RPC connects (the connect task is established).
        await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);
        Assert.Equal(1, server.Service.ConnectCalls);

        await client.UpdatePasswordAsync(
            new UpdatePasswordReq
            {
                UserName = "other_user",
                OldPassword = "old",
                NewPassword = "new",
                ResetConnection = true
            },
            TestContext.Current.CancellationToken);

        // resetConnection=true drops the shared connect task, so the next RPC reconnects and re-registers
        // the client as the updated user.
        await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);
        Assert.Equal(2, server.Service.ConnectCalls);
    }


    [Fact]
    public async Task QueryIterator_keyless_cursor_resumes_from_beginning()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 5;
        server.Service.NextQuerySessionTs = 100;

        // Consume one row to establish and capture the pinned snapshot.
        QueryIterator first = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1,
            Parameters = new QueryParameters { Limit = 1 }
        });
        await foreach (IReadOnlyList<FieldData> _ in first.WithCancellation(TestContext.Current.CancellationToken))
        {
        }

        QueryIteratorCursor? captured = first.GetCursor();
        Assert.NotNull(captured);
        Assert.Equal(100UL, captured!.SessionTs);

        // A keyless cursor (no pk) resumes from the beginning of the collection on that same snapshot.
        QueryIterator resumed = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1,
            Parameters = new QueryParameters { Limit = 3 },
            Cursor = new QueryIteratorCursor(captured.SessionTs)
        });

        int pages = 0;
        await foreach (IReadOnlyList<FieldData> _ in resumed.WithCancellation(TestContext.Current.CancellationToken))
        {
            pages++;
        }

        Assert.Equal(3, pages);
        Assert.Equal(4, server.Service.QueryRequests.Count);

        // The keyless resume's first request carries the full-range expression (no pk bound) and pins the
        // captured snapshot.
        Grpc.QueryRequest resumedRequest = server.Service.QueryRequests[1];
        Assert.Equal("", resumedRequest.Expr);
        Assert.Equal(captured.SessionTs, resumedRequest.GuaranteeTimestamp);
    }

    [Fact]
    public async Task HybridSearch_rejects_extra_parameters_order_by_fields()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        var parameters = new SearchParameters();
        parameters.ExtraParameters["order_by_fields"] = "title:desc";

        // The raw key is forwarded verbatim into RankParams where the hybrid request has no order_by wire
        // slot; reject it up front like the typed property.
        await Assert.ThrowsAsync<ArgumentException>(() => client.HybridSearchAsync(
            new HybridSearchReq
            {
                CollectionName = "coll",
                SearchRequests =
                [
                    new AnnSearchReq
                    {
                        VectorFieldName = "embedding",
                        Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 2f, 3f, 4f }) },
                        MetricType = MetricType.L2,
                        Limit = 10
                    }
                ],
                Limit = 3,
                Parameters = parameters
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task QueryIterator_reenumeration_restarts_from_beginning()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 5;
        server.Service.NextQuerySessionTs = 100;

        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1,
            Parameters = new QueryParameters { Limit = 2 }
        });

        int first = 0;
        await foreach (IReadOnlyList<FieldData> _ in iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
            first++;
        }

        Assert.Equal(2, first);

        // Re-enumerating the same iterator object starts a fresh iteration (fresh position) instead of
        // resuming from the previous run's last pk.
        int second = 0;
        await foreach (IReadOnlyList<FieldData> _ in iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
            second++;
        }

        Assert.Equal(2, second);

        // The second run's first request carries the full-range expression, not `id > <previous last pk>`.
        Grpc.QueryRequest secondRunFirst = server.Service.QueryRequests[2];
        Assert.Equal("", secondRunFirst.Expr);
    }

    [Fact]
    public async Task QueryIterator_element_filter_seek_resumes_mid_entity()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.NextQuerySessionTs = 100;
        server.Service.QueryElementIndices = new[] { new long[] { 0, 1 }, new long[] { 2, 3 }, new long[] { 4, 5 } };

        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 2,
            Expression = "element_filter(ArrayContains('clips', 'video'))",
            Parameters = new QueryParameters { Offset = 4, Limit = 2 }
        });

        int pages = 0;
        await foreach (IReadOnlyList<FieldData> _ in iterator.WithCancellation(TestContext.Current.CancellationToken))
        {
            pages++;
        }

        Assert.Equal(1, pages);
        Assert.Equal(3, server.Service.QueryRequests.Count);

        // The second seek batch resumes mid-entity: it sends the element cursor params and uses the inclusive
        // `>=` operator so the un-read tail of the partially-consumed entity is not dropped.
        Grpc.QueryRequest secondSeek = server.Service.QueryRequests[1];
        Assert.Contains("id >= 0", secondSeek.Expr);
        var byKey = secondSeek.QueryParams.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal("0", byKey["query_iter_last_pk"]);
        Assert.Equal("1", byKey["query_iter_last_element_offset"]);
    }

    [Fact]
    public async Task QueryIterator_zero_session_ts_cursor_does_not_pin()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        SchemaCache.Instance.Clear();
        server.Service.DescribeSchema = BuildIteratorSchema();
        server.Service.QueryIteratorTotalRows = 5;
        server.Service.NextQuerySessionTs = 100;

        // A cursor with SessionTs 0 does not pin a snapshot: the server picks its own per page.
        QueryIterator resumed = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = "iter_coll",
            BatchSize = 1,
            Parameters = new QueryParameters { Limit = 2 },
            Cursor = new QueryIteratorCursor(0UL, intPk: 2L)
        });

        int pages = 0;
        await foreach (IReadOnlyList<FieldData> _ in resumed.WithCancellation(TestContext.Current.CancellationToken))
        {
            pages++;
        }

        Assert.Equal(2, pages);

        // The first resumed request carries guarantee_timestamp 0 (server picks), ...
        Grpc.QueryRequest first = server.Service.QueryRequests[0];
        Assert.Equal(0UL, first.GuaranteeTimestamp);
        Assert.Equal("id > 2", first.Expr);
        // ...and the loop pins the server's returned SessionTs afterward, so a cursor captured from this
        // iterator carries a real snapshot.
        QueryIteratorCursor? captured = resumed.GetCursor();
        Assert.NotNull(captured);
        Assert.True(captured!.SessionTs > 0);
    }

    private static CollectionSchema BuildIteratorSchema()
    {
        var schema = new CollectionSchema { Name = "iter_coll" };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateFloatVector("embedding", 4));
        return schema;
    }
}
