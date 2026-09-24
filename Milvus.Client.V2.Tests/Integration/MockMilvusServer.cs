using Grpc.Net.Client;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Types;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Tests.Integration;

/// <summary>
/// An in-process mock of the Milvus gRPC service, used to exercise the <see cref="MilvusClientV2" />
/// facade without a real Milvus server.
/// </summary>
internal sealed class MockMilvusService : MilvusService.MilvusServiceBase
{
    /// <summary>
    /// The value to return from <c>HasCollection</c>.
    /// </summary>
    public bool HasCollectionResult { get; set; }

    /// <summary>
    /// The collection names to return from <c>ListCollections</c>.
    /// </summary>
    public List<string> CollectionNames { get; } = new();

    /// <summary>
    /// If set, every operation fails with this status instead of succeeding.
    /// </summary>
    public Milvus.Client.Grpc.Status? FailureStatus { get; set; }

    /// <summary>
    /// If greater than zero, the next operations fail with <see cref="FailureStatus" /> (or a rate-limit status
    /// when <see cref="FailureStatus" /> is null), then succeed — used to exercise the retry policy.
    /// </summary>
    public int FailNextCalls { get; set; }

    public int TotalCalls { get; private set; }

    /// <summary>
    /// If greater than zero, the next mutation RPCs fail with a <c>SchemaMismatch</c> status before the normal
    /// status logic applies — used to exercise the insert/upsert schema-mismatch single retry.
    /// </summary>
    public int FailNextMutationsWithSchemaMismatch { get; set; }

    private Milvus.Client.Grpc.Status NextStatus()
    {
        TotalCalls++;
        if (RpcFailure is not null && FailNextCalls > 0)
        {
            FailNextCalls--;
            throw RpcFailure;
        }

        if (FailNextCalls > 0)
        {
            FailNextCalls--;
            return FailureStatus ?? new Milvus.Client.Grpc.Status { Code = (int)MilvusErrorCode.RateLimit, Reason = "rate limited" };
        }

        return FailureStatus ?? OkStatus;
    }

    // Mutation RPCs (insert/upsert) first honor the schema-mismatch flag, then fall back to NextStatus.
    private Milvus.Client.Grpc.Status MutationStatus()
    {
        if (FailNextMutationsWithSchemaMismatch > 0)
        {
            FailNextMutationsWithSchemaMismatch--;
            TotalCalls++;
            return new Milvus.Client.Grpc.Status { Code = (int)MilvusErrorCode.SchemaMismatch, Reason = "schema mismatch" };
        }

        return NextStatus();
    }

    /// <summary>
    /// When set, each RPC throws an <see cref="RpcException" /> (transport-level failure) instead of returning a
    /// status, used to exercise the transport (RpcException) branch of the retry policy.
    /// </summary>
    public RpcException? RpcFailure { get; set; }

    /// <summary>
    /// The most recent request object received by any RPC (used to assert request forwarding).
    /// </summary>
    public object? LastRequest { get; private set; }

    /// <summary>
    /// The most recent request received by each specific RPC, keyed by the RPC method name.
    /// </summary>
    public Dictionary<string, object> Requests { get; } = new();

    private TRecord Record<TRecord>(string rpc, TRecord request)
    {
        LastRequest = request;
        Requests[rpc] = request!;
        return request;
    }

    public string? LastCreatedCollectionName { get; private set; }
    public string? LastDroppedCollectionName { get; private set; }
    public string? LastCheckedCollectionName { get; private set; }

    /// <summary>
    /// The value of the <c>dbname</c> header on the most recent <c>HasCollection</c> request.
    /// </summary>
    public string? LastRequestDbName { get; private set; }
    public Milvus.Client.Grpc.ClientInfo? LastConnectClientInfo { get; private set; }

    public override Task<Milvus.Client.Grpc.Status> CreateCollection(
        CreateCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(CreateCollection), request);
        LastCreatedCollectionName = request.CollectionName;
        return Task.FromResult(NextStatus());
    }

    public override Task<Milvus.Client.Grpc.Status> DropCollection(
        DropCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(DropCollection), request);
        LastDroppedCollectionName = request.CollectionName;
        return Task.FromResult(NextStatus());
    }

    public override Task<BoolResponse> HasCollection(HasCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(HasCollection), request);
        LastCheckedCollectionName = request.CollectionName;
        LastRequestDbName = context.RequestHeaders.GetValue("dbname");
        return Task.FromResult(new BoolResponse
        {
            Status = NextStatus(),
            Value = HasCollectionResult
        });
    }

    public override Task<ShowCollectionsResponse> ShowCollections(
        ShowCollectionsRequest request, ServerCallContext context)
    {
        Record(nameof(ShowCollections), request);
        var response = new ShowCollectionsResponse { Status = NextStatus() };
        response.CollectionNames.AddRange(CollectionNames);
        return Task.FromResult(response);
    }

    /// <summary>
    /// The schema returned by <c>DescribeCollection</c> (used by Get/Describe flows).
    /// </summary>
    public Milvus.Client.V2.Types.CollectionSchema? DescribeSchema { get; set; }

    public override Task<DescribeCollectionResponse> DescribeCollection(
        DescribeCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(DescribeCollection), request);
        DescribeCollectionCount++;
        var response = new DescribeCollectionResponse { Status = NextStatus() };
        response.Schema = new Grpc.CollectionSchema
        {
            Name = DescribeSchema?.Name ?? request.CollectionName,
            EnableDynamicField = DescribeSchema?.EnableDynamicFields ?? false
        };
        response.CollectionID = DescribeCollectionId;
        response.UpdateTimestamp = DescribeUpdateTimestamp;
        foreach (Milvus.Client.V2.Types.FieldSchema field in DescribeSchema?.Fields ?? [])
        {
            var grpcField = new Grpc.FieldSchema
            {
                Name = field.Name,
                DataType = (Grpc.DataType)(int)field.DataType,
                IsPrimaryKey = field.IsPrimaryKey,
                AutoID = field.AutoId,
                IsPartitionKey = field.IsPartitionKey,
                IsClusteringKey = field.IsClusteringKey,
                Nullable = field.Nullable,
                IsDynamic = field.IsDynamic
            };
            if (field.MaxLength is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = "max_length", Value = field.MaxLength.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (field.Dimension is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = "dim", Value = field.Dimension.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (field.ElementDataType is not null)
            {
                grpcField.ElementType = (Grpc.DataType)(int)field.ElementDataType.Value;
            }
            if (field.MaxCapacity is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = "max_capacity", Value = field.MaxCapacity.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (field.DefaultValue is not null)
            {
                grpcField.DefaultValue = Milvus.Client.V2.Requests.Collection.CreateCollectionReq.ConvertToValueField(
                    field.DefaultValue, field.DataType);
            }
            if (field.EnableAnalyzer)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair { Key = "enable_analyzer", Value = "true" });
            }
            if (field.AnalyzerParams is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = "analyzer_params",
                    Value = System.Text.Json.JsonSerializer.Serialize(field.AnalyzerParams)
                });
            }
            response.Schema.Fields.Add(grpcField);
        }

        foreach (Milvus.Client.V2.Types.StructFieldSchema structField in DescribeSchema?.StructFields ?? [])
        {
            var grpcStruct = new Grpc.StructArrayFieldSchema
            {
                Name = structField.Name,
                Description = structField.Description
            };
            foreach (Milvus.Client.V2.Types.FieldSchema subField in structField.Fields)
            {
                var grpcSubField = new Grpc.FieldSchema
                {
                    Name = subField.Name,
                    ElementType = (Grpc.DataType)(int)subField.DataType,
                    DataType = IsVectorType(subField.DataType) ? Grpc.DataType.ArrayOfVector : Grpc.DataType.Array
                };
                grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = "max_capacity", Value = structField.MaxCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                if (subField.MaxLength is not null)
                {
                    grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = "max_length", Value = subField.MaxLength.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }
                if (subField.Dimension is not null)
                {
                    grpcSubField.TypeParams.Add(new Grpc.KeyValuePair { Key = "dim", Value = subField.Dimension.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }
                grpcStruct.Fields.Add(grpcSubField);
            }
            response.Schema.StructArrayFields.Add(grpcStruct);
        }

        return Task.FromResult(response);
    }

    public override Task<BatchDescribeCollectionResponse> BatchDescribeCollection(
        BatchDescribeCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(BatchDescribeCollection), request);
        var response = new BatchDescribeCollectionResponse { Status = NextStatus() };
        foreach (string collectionName in request.CollectionName)
        {
            var desc = new DescribeCollectionResponse
            {
                Status = NextStatus(),
                CollectionName = collectionName,
                CollectionID = DescribeCollectionId,
                Schema = new Grpc.CollectionSchema { Name = collectionName }
            };

            if (BatchDescribeMissingCollections?.Contains(collectionName) == true)
            {
                desc.Status = new Milvus.Client.Grpc.Status { Code = 100, Reason = $"collection not found: {collectionName}" };
            }

            response.Responses.Add(desc);
        }

        return Task.FromResult(response);
    }

    /// <summary>
    /// The collection ID returned by <c>DescribeCollection</c> (default 100); CompactAsync/OptimizeAsync
    /// forward this as the ManualCompactionRequest.CollectionID.
    /// </summary>
    public long DescribeCollectionId { get; set; } = 100;

    /// <summary>
    /// The UpdateTimestamp returned by DescribeCollection. Non-zero by default so the schema-timestamp
    /// stamping in InsertAsync/UpsertAsync sends a real SchemaTimestamp on the wire (exercising the stale
    /// schema/SchemaMismatch path rather than always stamping 0).
    /// </summary>
    public ulong DescribeUpdateTimestamp { get; set; } = 1000;

    /// <summary>
    /// When set, BatchDescribeCollection returns a per-response failure (code != 0) for the listed collections,
    /// simulating a requested collection that does not exist.
    /// </summary>
    public IReadOnlySet<string>? BatchDescribeMissingCollections { get; set; }

    /// <summary>
    /// When true, DescribeIndex returns IndexNotFound (code 700), simulating a collection that has no index.
    /// </summary>
    public bool DescribeIndexNotFound { get; set; }

    public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
    {
        Record(nameof(Connect), request);
        LastConnectClientInfo = request.ClientInfo;
        var response = new ConnectResponse { Status = NextStatus() };
        response.ServerInfo = new ServerInfo { BuildTags = ServerVersion };
        return Task.FromResult(response);
    }

    public override Task<GetVersionResponse> GetVersion(GetVersionRequest request, ServerCallContext context)
    {
        Record(nameof(GetVersion), request);
        return Task.FromResult(new GetVersionResponse { Status = NextStatus(), Version = ServerVersion });
    }

    /// <summary>
    /// The server version reported by <c>GetVersion</c> and <c>Connect</c>.
    /// </summary>
    public string ServerVersion { get; set; } = "v2.6.0";

    /// <summary>
    /// The timestamp returned by mutation RPCs (used to test the ts cache).
    /// </summary>
    public ulong NextMutationTimestamp { get; set; } = 12345;

    public string? LastInsertedCollection { get; private set; }
    public int LastInsertedRows { get; private set; }
    public IReadOnlyList<Milvus.Client.Grpc.FieldData>? LastInsertedFieldData { get; private set; }
    public string? LastDeletedCollection { get; private set; }
    public string? LastDeleteExpression { get; private set; }

    public override Task<MutationResult> Insert(InsertRequest request, ServerCallContext context)
    {
        Record(nameof(Insert), request);
        LastInsertedCollection = request.CollectionName;
        LastInsertedRows = (int)request.NumRows;
        LastInsertedFieldData = request.FieldsData.ToList();
        ValidateMutationAgainstSchema(request.FieldsData, (int)request.NumRows, request.CollectionName);
        var result = new MutationResult { Status = MutationStatus(), Timestamp = NextMutationTimestamp };
        result.InsertCnt = request.NumRows;
        return Task.FromResult(result);
    }

    public override Task<MutationResult> Upsert(UpsertRequest request, ServerCallContext context)
    {
        Record(nameof(Upsert), request);
        LastInsertedCollection = request.CollectionName;
        LastInsertedRows = (int)request.NumRows;
        ValidateMutationAgainstSchema(request.FieldsData, (int)request.NumRows, request.CollectionName);
        var result = new MutationResult { Status = MutationStatus(), Timestamp = NextMutationTimestamp };
        result.UpsertCnt = request.NumRows;
        return Task.FromResult(result);
    }

    // Light schema-based validation of the mutation's field data when a DescribeSchema is configured: catches
    // wire-encoding regressions (wrong vector dimension, dropped/unknown field) that a permissive mock would
    // otherwise accept. Skips validation entirely when no DescribeSchema is set (schema-less tests).
    private void ValidateMutationAgainstSchema(
        IEnumerable<Milvus.Client.Grpc.FieldData> fieldsData, int numRows, string collectionName)
    {
        Milvus.Client.V2.Types.CollectionSchema? schema = DescribeSchema;
        if (schema is null)
        {
            return;
        }

        foreach (Milvus.Client.Grpc.FieldData fieldData in fieldsData)
        {
            if (fieldData.IsDynamic || fieldData.FieldName.Length == 0)
            {
                continue;
            }

            Milvus.Client.V2.Types.FieldSchema? field = schema.Fields.FirstOrDefault(f => f.Name == fieldData.FieldName)
                ?? schema.StructFields.SelectMany(sf => sf.Fields).FirstOrDefault(f => f.Name == fieldData.FieldName);
            if (field is null)
            {
                // A struct container field (e.g. a row's "st" struct) is valid when a StructFieldSchema names it;
                // its packed sub-fields are carried inside the struct ArrayArray, not as top-level fields.
                bool isStructContainer = schema.StructFields.Any(sf => sf.Name == fieldData.FieldName);
                if (!isStructContainer)
                {
                    throw new RpcException(new global::Grpc.Core.Status(StatusCode.InvalidArgument,
                        $"Field '{fieldData.FieldName}' is not part of collection '{collectionName}'."));
                }

                continue;
            }

            if (fieldData.Vectors is not null && field.Dimension is { } expectedDim)
            {
                int actualDim = VectorDim(fieldData.Vectors, numRows);
                if (actualDim != expectedDim)
                {
                    throw new RpcException(new global::Grpc.Core.Status(StatusCode.InvalidArgument,
                        $"Field '{fieldData.FieldName}' has vector dimension {actualDim}, expected {expectedDim}."));
                }
            }
        }
    }

    private static int VectorDim(Milvus.Client.Grpc.VectorField vectors, int numRows)
        => vectors.DataCase switch
        {
            // Dense float vectors for all rows are flattened into one FloatArray; the dimension is the total
            // element count divided by the number of rows.
            Milvus.Client.Grpc.VectorField.DataOneofCase.FloatVector => vectors.FloatVector.Data.Count / Math.Max(1, numRows),
            Milvus.Client.Grpc.VectorField.DataOneofCase.Float16Vector => vectors.Float16Vector.Length / 2 / Math.Max(1, numRows),
            Milvus.Client.Grpc.VectorField.DataOneofCase.Bfloat16Vector => vectors.Bfloat16Vector.Length / 2 / Math.Max(1, numRows),
            Milvus.Client.Grpc.VectorField.DataOneofCase.Int8Vector => vectors.Int8Vector.Length / Math.Max(1, numRows),
            Milvus.Client.Grpc.VectorField.DataOneofCase.BinaryVector => vectors.BinaryVector.Length * 8 / Math.Max(1, numRows),
            Milvus.Client.Grpc.VectorField.DataOneofCase.SparseFloatVector => (int)vectors.Dim,
            _ => 0
        };

    public override Task<MutationResult> Delete(DeleteRequest request, ServerCallContext context)
    {
        Record(nameof(Delete), request);
        LastDeletedCollection = request.CollectionName;
        LastDeleteExpression = request.Expr;
        var result = new MutationResult { Status = NextStatus(), Timestamp = NextMutationTimestamp };
        result.DeleteCnt = 1;
        return Task.FromResult(result);
    }

    public string? LastSearchedCollection { get; private set; }
    public int LastSearchTopK { get; private set; }
    public ulong? LastSearchGuaranteeTimestamp { get; private set; }
    public bool LastSearchUseDefaultConsistency { get; private set; }
    public IReadOnlyList<KeyValuePair<string, string>> LastSearchParams { get; private set; } = [];

    /// <summary>
    /// When set, <c>Search</c> returns a search-iterator token on every page, letting SearchIteratorCoreAsync
    /// run its success path; the iteration ends via the request's remaining count.
    /// </summary>
    public string? SearchIteratorToken { get; set; }

    /// <summary>
    /// Every <c>Search</c> request received, in order (the shared <see cref="Requests" /> dictionary keeps only
    /// the last one, so page-by-page assertions use this list).
    /// </summary>
    public List<SearchRequest> SearchRequests { get; } = new();

    /// <summary>
    /// Every <c>CreateIndex</c> request received, in call order (the <see cref="Requests" /> dictionary keeps
    /// only the last call per RPC name, so multi-index creates need this list).
    /// </summary>
    public List<CreateIndexRequest> CreateIndexRequests { get; } = new();

    private SearchResults BuildSearchResults(string collectionName, int topK)
    {
        var results = new SearchResults
        {
            Status = SearchMetricsStatus ?? NextStatus(),
            CollectionName = collectionName,
            Results = new SearchResultData { NumQueries = 1, TopK = topK },
            // A snapshot timestamp, letting the search-iterator pin it for the next page.
            SessionTs = NextSearchSessionTs
        };
        results.Results.Scores.AddRange(Enumerable.Repeat(0.5f, topK));
        results.Results.Ids = new Milvus.Client.Grpc.IDs();
        results.Results.Ids.IntId = new Milvus.Client.Grpc.LongArray();
        results.Results.Ids.IntId.Data.AddRange(Enumerable.Range(0, topK).Select(i => (long)(i + 1)));
        // Per-query result count, used by the search-iterator loop.
        results.Results.Topks.Add(topK);
        return results;
    }

    /// <summary>
    /// The status carried by mock <c>Search</c> results (defaults to <see cref="NextStatus()" />). Lets tests
    /// assert the search metrics the client decodes from <c>status.extra_info</c>.
    /// </summary>
    public Milvus.Client.Grpc.Status? SearchMetricsStatus { get; set; }

    /// <summary>
    /// The SessionTs reported by the mock <c>Search</c> results (default 0, letting the client pin its own
    /// snapshot).
    /// </summary>
    public ulong NextSearchSessionTs { get; set; }

    /// <summary>
    /// The SessionTs reported by the mock <c>Query</c> results (default 0, letting the client pin its own
    /// snapshot), advancing on every Query call so a correctly pinning query-iterator keeps page 1's value.
    /// </summary>
    public ulong NextQuerySessionTs { get; set; }

    public override Task<SearchResults> Search(SearchRequest request, ServerCallContext context)
    {
        Record(nameof(Search), request);
        SearchRequests.Add(request);
        LastSearchedCollection = request.CollectionName;
        LastSearchTopK = int.Parse(request.SearchParams.Single(p => p.Key == "topk").Value, System.Globalization.CultureInfo.InvariantCulture);
        LastSearchGuaranteeTimestamp = request.GuaranteeTimestamp;
        LastSearchUseDefaultConsistency = request.UseDefaultConsistency;
        LastSearchParams = request.SearchParams.Select(p => new KeyValuePair<string, string>(p.Key, p.Value)).ToList();

        SearchResults results = BuildSearchResults(request.CollectionName, LastSearchTopK);
        if (SearchIteratorToken is not null)
        {
            // Keep returning the token on every page; the iteration loop ends via its `remaining` count.
            results.Results.SearchIteratorV2Results = new Grpc.SearchIteratorV2Results
            {
                Token = SearchIteratorToken,
                LastBound = 1.5f
            };
        }

        // Advance the reported snapshot so subsequent pages carry a different SessionTs; a correctly pinning
        // iterator keeps page 1's value on page 2.
        NextSearchSessionTs++;

        return Task.FromResult(results);
    }

    public override Task<SearchResults> HybridSearch(HybridSearchRequest request, ServerCallContext context)
    {
        Record(nameof(HybridSearch), request);
        int topK = request.RankParams.Any(p => p.Key == "limit")
            ? int.Parse(request.RankParams.Single(p => p.Key == "limit").Value, System.Globalization.CultureInfo.InvariantCulture)
            : 1;
        return Task.FromResult(BuildSearchResults(request.CollectionName, topK));
    }

    public string? LastQueriedCollection { get; private set; }
    public string? LastQueryExpression { get; private set; }
    public ulong? LastQueryGuaranteeTimestamp { get; private set; }
    public int LastQueryCount { get; private set; }
    public int DescribeCollectionCount { get; private set; }
    public IReadOnlyList<string> LastQueryLimit => _lastQueryLimits;
    private readonly List<string> _lastQueryLimits = [];

    /// <summary>
    /// When set, the mock <c>Query</c> returns primary-key rows advancing one page per call (used to exercise
    /// the query-iterator cursor advancement). The initial expression must be a lower-bound on the pk; the mock
    /// parses the <c>id &gt; last</c> cursor from subsequent pages. When unset (default), a single row (id=1) is
    /// returned, preserving the historical behavior.
    /// </summary>
    public int? QueryIteratorTotalRows { get; set; }

    /// <summary>
    /// When set, the mock <c>Query</c> returns this many extra rows beyond the requested limit on every page,
    /// simulating a server that over-delivers under <c>reduce_stop_for_best</c>.
    /// </summary>
    public int QueryOverDeliver { get; set; }

    /// <summary>
    /// When set, the mock <c>Query</c> returns VarChar primary keys (used to exercise the query-iterator
    /// string cursor). Advancing one page per call, starting from the lower bound parsed out of the expression
    /// (e.g. <c>id &gt; 'pk_0002'</c>).
    /// </summary>
    public int? QueryIteratorStringPkRows { get; set; }

    public override Task<QueryResults> Query(QueryRequest request, ServerCallContext context)
    {
        Record(nameof(Query), request);
        LastQueriedCollection = request.CollectionName;
        LastQueryExpression = request.Expr;
        LastQueryGuaranteeTimestamp = request.GuaranteeTimestamp;
        LastQueryCount++;
        _lastQueryLimits.Add(ParseQueryParamLimit(request)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "");

        var results = new QueryResults { Status = NextStatus(), CollectionName = request.CollectionName };
        // Report a snapshot timestamp (advancing per call) so the query-iterator's SessionTs pinning branch is
        // exercised; a 0 value falls back to the client-side snapshot as before.
        results.SessionTs = NextQuerySessionTs++;

        if (QueryIteratorStringPkRows is { } stringRows)
        {
            var pkField = new Grpc.FieldData { FieldName = "id", Type = Grpc.DataType.VarChar };
            pkField.Scalars = new Grpc.ScalarField
            {
                StringData = new Grpc.StringArray()
            };

            string from = ParsePkStringLowerBound(request.Expr);
            int limit = (ParseQueryParamLimit(request) ?? 1000) + QueryOverDeliver;
            for (int i = 0; i < limit; i++)
            {
                string? next = StringPkValue(stringRows, from, i);
                if (next is null)
                {
                    break;
                }

                pkField.Scalars.StringData.Data.Add(next);
            }

            results.FieldsData.Add(pkField);
            return Task.FromResult(results);
        }

        var idField = new Grpc.FieldData { FieldName = "id", Type = Grpc.DataType.Int64 };
        idField.Scalars = new Grpc.ScalarField();
        idField.Scalars.LongData = new Grpc.LongArray();

        if (QueryIteratorTotalRows is { } totalRows)
        {
            // Parse the lower bound from the expression ("id >= N" first page, "id > N and (...)" later pages).
            long from = ParsePkLowerBound(request.Expr);
            int limit = (ParseQueryParamLimit(request) ?? 1000) + QueryOverDeliver;
            for (int i = 0; i < limit && from + i < totalRows; i++)
            {
                idField.Scalars.LongData.Data.Add(from + i);
            }
        }
        else
        {
            idField.Scalars.LongData.Data.Add(1);
        }

        results.FieldsData.Add(idField);
        return Task.FromResult(results);
    }

    private static long ParsePkLowerBound(string expr)
    {
        int idx = expr.IndexOf("id > ", StringComparison.Ordinal);
        if (idx < 0)
        {
            idx = expr.IndexOf("id >= ", StringComparison.Ordinal);
            if (idx < 0)
            {
                return 0;
            }

            idx += "id >= ".Length;
        }
        else
        {
            idx += "id > ".Length;
        }

        int end = expr.IndexOfAny(new[] { ' ', ')' }, idx);
        if (end < 0)
        {
            end = expr.Length;
        }

        long parsed = long.Parse(expr.AsSpan(idx, end - idx), System.Globalization.CultureInfo.InvariantCulture);
        // The iterator's initial expression is "id >= <min int64>"; normalize that to a start from 0 so the mock
        // returns a small, countable row sequence.
        return parsed == long.MinValue ? 0 : parsed + 1;
    }

    private static string ParsePkStringLowerBound(string expr)
    {
        int idx = expr.IndexOf("id > '", StringComparison.Ordinal);
        if (idx < 0)
        {
            idx = expr.IndexOf("id >= '", StringComparison.Ordinal);
            if (idx < 0)
            {
                return "";
            }

            idx += "id >= '".Length;
        }
        else
        {
            idx += "id > '".Length;
        }

        int end = expr.IndexOf("'", idx, StringComparison.Ordinal);
        if (end < 0)
        {
            end = expr.Length;
        }

        return expr.Substring(idx, end - idx);
    }

    // Yields pk_0001, pk_0002, ... starting after the given lower bound, wrapping the seek back to pk_0001 when
    // a synthetic ">= min" start is used. Returns null once the full sequence has been emitted.
    private static string? StringPkValue(int totalRows, string from, int offset)
    {
        if (string.IsNullOrEmpty(from))
        {
            from = "pk_0000";
        }

        int fromIndex = int.Parse(from.AsSpan(3), System.Globalization.CultureInfo.InvariantCulture);
        int valueIndex = fromIndex + offset + 1;
        return valueIndex <= totalRows ? $"pk_{valueIndex:0000}" : null;
    }

    private static int? ParseQueryParamLimit(QueryRequest request)
    {
        foreach (Grpc.KeyValuePair pair in request.QueryParams)
        {
            if (pair.Key == "limit")
            {
                return int.Parse(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    public override Task<CheckHealthResponse> CheckHealth(CheckHealthRequest request, ServerCallContext context)
    {
        Record(nameof(CheckHealth), request);
        var response = new CheckHealthResponse { Status = NextStatus(), IsHealthy = true };
        response.Reasons.Add("healthy");
        response.QuotaStates.Add(Milvus.Client.Grpc.QuotaState.ReadLimited);
        return Task.FromResult(response);
    }

    public override Task<GetCollectionStatisticsResponse> GetCollectionStatistics(
        GetCollectionStatisticsRequest request, ServerCallContext context)
    {
        Record(nameof(GetCollectionStatistics), request);
        var response = new GetCollectionStatisticsResponse { Status = NextStatus() };
        response.Stats.Add(new Grpc.KeyValuePair { Key = "row_count", Value = "0" });
        return Task.FromResult(response);
    }

    public override Task<GetLoadStateResponse> GetLoadState(GetLoadStateRequest request, ServerCallContext context)
    {
        Record(nameof(GetLoadState), request);
        return Task.FromResult(new GetLoadStateResponse { Status = NextStatus(), State = GetLoadStateResult });
    }

    /// <summary>
    /// The load state returned by <c>GetLoadState</c> (default <see cref="Grpc.LoadState.Loaded" />).
    /// </summary>
    public Grpc.LoadState GetLoadStateResult { get; set; } = Grpc.LoadState.Loaded;

    /// <summary>
    /// The progress returned by <c>GetLoadingProgress</c>.
    /// </summary>
    public long LoadingProgress { get; set; } = 42;

    public override Task<GetLoadingProgressResponse> GetLoadingProgress(
        GetLoadingProgressRequest request, ServerCallContext context)
    {
        Record(nameof(GetLoadingProgress), request);
        return Task.FromResult(new GetLoadingProgressResponse { Status = NextStatus(), Progress = LoadingProgress });
    }

    public override Task<Grpc.Status> RenameCollection(RenameCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(RenameCollection), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> LoadCollection(LoadCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(LoadCollection), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> ReleaseCollection(ReleaseCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(ReleaseCollection), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AddCollectionField(AddCollectionFieldRequest request, ServerCallContext context)
    {
        Record(nameof(AddCollectionField), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AddCollectionFunction(AddCollectionFunctionRequest request, ServerCallContext context)
    {
        Record(nameof(AddCollectionFunction), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterCollectionFunction(AlterCollectionFunctionRequest request, ServerCallContext context)
    {
        Record(nameof(AlterCollectionFunction), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropCollectionFunction(DropCollectionFunctionRequest request, ServerCallContext context)
    {
        Record(nameof(DropCollectionFunction), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterCollectionField(AlterCollectionFieldRequest request, ServerCallContext context)
    {
        Record(nameof(AlterCollectionField), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterCollection(AlterCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(AlterCollection), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<GetReplicasResponse> GetReplicas(GetReplicasRequest request, ServerCallContext context)
    {
        Record(nameof(GetReplicas), request);
        return Task.FromResult(new GetReplicasResponse { Status = NextStatus() });
    }

    public override Task<TruncateCollectionResponse> TruncateCollection(TruncateCollectionRequest request, ServerCallContext context)
    {
        Record(nameof(TruncateCollection), request);
        return Task.FromResult(new TruncateCollectionResponse { Status = NextStatus() });
    }

    // ---- Alias ----

    public override Task<Grpc.Status> CreateAlias(CreateAliasRequest request, ServerCallContext context)
    {
        Record(nameof(CreateAlias), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropAlias(DropAliasRequest request, ServerCallContext context)
    {
        Record(nameof(DropAlias), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterAlias(AlterAliasRequest request, ServerCallContext context)
    {
        Record(nameof(AlterAlias), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<DescribeAliasResponse> DescribeAlias(DescribeAliasRequest request, ServerCallContext context)
    {
        Record(nameof(DescribeAlias), request);
        return Task.FromResult(new DescribeAliasResponse { Status = NextStatus(), Alias = request.Alias });
    }

    public override Task<ListAliasesResponse> ListAliases(ListAliasesRequest request, ServerCallContext context)
    {
        Record(nameof(ListAliases), request);
        return Task.FromResult(new ListAliasesResponse { Status = NextStatus() });
    }

    // ---- Database ----

    public override Task<Grpc.Status> CreateDatabase(CreateDatabaseRequest request, ServerCallContext context)
    {
        Record(nameof(CreateDatabase), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropDatabase(DropDatabaseRequest request, ServerCallContext context)
    {
        Record(nameof(DropDatabase), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterDatabase(AlterDatabaseRequest request, ServerCallContext context)
    {
        Record(nameof(AlterDatabase), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<ListDatabasesResponse> ListDatabases(ListDatabasesRequest request, ServerCallContext context)
    {
        Record(nameof(ListDatabases), request);
        return Task.FromResult(new ListDatabasesResponse { Status = NextStatus() });
    }

    public override Task<DescribeDatabaseResponse> DescribeDatabase(DescribeDatabaseRequest request, ServerCallContext context)
    {
        Record(nameof(DescribeDatabase), request);
        return Task.FromResult(new DescribeDatabaseResponse { Status = NextStatus(), DbName = request.DbName });
    }

    // ---- Index ----

    public override Task<Grpc.Status> CreateIndex(CreateIndexRequest request, ServerCallContext context)
    {
        Record(nameof(CreateIndex), request);
        CreateIndexRequests.Add(request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropIndex(DropIndexRequest request, ServerCallContext context)
    {
        Record(nameof(DropIndex), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterIndex(AlterIndexRequest request, ServerCallContext context)
    {
        Record(nameof(AlterIndex), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<DescribeIndexResponse> DescribeIndex(DescribeIndexRequest request, ServerCallContext context)
    {
        Record(nameof(DescribeIndex), request);
        if (DescribeIndexNotFound)
        {
            return Task.FromResult(new DescribeIndexResponse
            {
                Status = new Milvus.Client.Grpc.Status { Code = 700, Reason = "index not found" }
            });
        }

        var response = new DescribeIndexResponse { Status = NextStatus() };
        response.IndexDescriptions.Add(new Milvus.Client.Grpc.IndexDescription
        {
            IndexName = "idx",
            FieldName = request.FieldName,
            State = Milvus.Client.Grpc.IndexState.Finished
        });
        return Task.FromResult(response);
    }

    // ---- Partition ----

    public override Task<Grpc.Status> CreatePartition(CreatePartitionRequest request, ServerCallContext context)
    {
        Record(nameof(CreatePartition), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropPartition(DropPartitionRequest request, ServerCallContext context)
    {
        Record(nameof(DropPartition), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<BoolResponse> HasPartition(HasPartitionRequest request, ServerCallContext context)
    {
        Record(nameof(HasPartition), request);
        return Task.FromResult(new BoolResponse { Status = NextStatus(), Value = true });
    }

    public override Task<ShowPartitionsResponse> ShowPartitions(ShowPartitionsRequest request, ServerCallContext context)
    {
        Record(nameof(ShowPartitions), request);
        return Task.FromResult(new ShowPartitionsResponse { Status = NextStatus() });
    }

    public override Task<Grpc.Status> LoadPartitions(LoadPartitionsRequest request, ServerCallContext context)
    {
        Record(nameof(LoadPartitions), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> ReleasePartitions(ReleasePartitionsRequest request, ServerCallContext context)
    {
        Record(nameof(ReleasePartitions), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<GetPartitionStatisticsResponse> GetPartitionStatistics(
        GetPartitionStatisticsRequest request, ServerCallContext context)
    {
        Record(nameof(GetPartitionStatistics), request);
        return Task.FromResult(new GetPartitionStatisticsResponse { Status = NextStatus() });
    }

    // ---- RBAC ----

    public override Task<Grpc.Status> CreateCredential(CreateCredentialRequest request, ServerCallContext context)
    {
        Record(nameof(CreateCredential), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DeleteCredential(DeleteCredentialRequest request, ServerCallContext context)
    {
        Record(nameof(DeleteCredential), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> UpdateCredential(UpdateCredentialRequest request, ServerCallContext context)
    {
        Record(nameof(UpdateCredential), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<ListCredUsersResponse> ListCredUsers(ListCredUsersRequest request, ServerCallContext context)
    {
        Record(nameof(ListCredUsers), request);
        var response = new ListCredUsersResponse { Status = NextStatus() };
        response.Usernames.Add("root");
        return Task.FromResult(response);
    }

    public override Task<SelectUserResponse> SelectUser(SelectUserRequest request, ServerCallContext context)
    {
        Record(nameof(SelectUser), request);
        return Task.FromResult(new SelectUserResponse { Status = NextStatus() });
    }

    public override Task<Grpc.Status> CreateRole(CreateRoleRequest request, ServerCallContext context)
    {
        Record(nameof(CreateRole), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropRole(DropRoleRequest request, ServerCallContext context)
    {
        Record(nameof(DropRole), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<SelectRoleResponse> SelectRole(SelectRoleRequest request, ServerCallContext context)
    {
        Record(nameof(SelectRole), request);
        return Task.FromResult(new SelectRoleResponse { Status = NextStatus() });
    }

    public override Task<Grpc.Status> OperateUserRole(OperateUserRoleRequest request, ServerCallContext context)
    {
        Record(nameof(OperateUserRole), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> OperatePrivilege(OperatePrivilegeRequest request, ServerCallContext context)
    {
        Record(nameof(OperatePrivilege), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<SelectGrantResponse> SelectGrant(SelectGrantRequest request, ServerCallContext context)
    {
        Record(nameof(SelectGrant), request);
        return Task.FromResult(new SelectGrantResponse { Status = NextStatus() });
    }

    public override Task<Grpc.Status> CreatePrivilegeGroup(CreatePrivilegeGroupRequest request, ServerCallContext context)
    {
        Record(nameof(CreatePrivilegeGroup), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropPrivilegeGroup(DropPrivilegeGroupRequest request, ServerCallContext context)
    {
        Record(nameof(DropPrivilegeGroup), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<ListPrivilegeGroupsResponse> ListPrivilegeGroups(ListPrivilegeGroupsRequest request, ServerCallContext context)
    {
        Record(nameof(ListPrivilegeGroups), request);
        return Task.FromResult(new ListPrivilegeGroupsResponse { Status = NextStatus() });
    }

    public override Task<Grpc.Status> OperatePrivilegeGroup(OperatePrivilegeGroupRequest request, ServerCallContext context)
    {
        Record(nameof(OperatePrivilegeGroup), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> OperatePrivilegeV2(OperatePrivilegeV2Request request, ServerCallContext context)
    {
        Record(nameof(OperatePrivilegeV2), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> AlterRole(AlterRoleRequest request, ServerCallContext context)
    {
        Record(nameof(AlterRole), request);
        return Task.FromResult(NextStatus());
    }

    // ---- Resource group ----

    public override Task<Grpc.Status> CreateResourceGroup(CreateResourceGroupRequest request, ServerCallContext context)
    {
        Record(nameof(CreateResourceGroup), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> DropResourceGroup(DropResourceGroupRequest request, ServerCallContext context)
    {
        Record(nameof(DropResourceGroup), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> UpdateResourceGroups(UpdateResourceGroupsRequest request, ServerCallContext context)
    {
        Record(nameof(UpdateResourceGroups), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> TransferNode(TransferNodeRequest request, ServerCallContext context)
    {
        Record(nameof(TransferNode), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<Grpc.Status> TransferReplica(TransferReplicaRequest request, ServerCallContext context)
    {
        Record(nameof(TransferReplica), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<ListResourceGroupsResponse> ListResourceGroups(ListResourceGroupsRequest request, ServerCallContext context)
    {
        Record(nameof(ListResourceGroups), request);
        return Task.FromResult(new ListResourceGroupsResponse { Status = NextStatus() });
    }

    public override Task<DescribeResourceGroupResponse> DescribeResourceGroup(
        DescribeResourceGroupRequest request, ServerCallContext context)
    {
        Record(nameof(DescribeResourceGroup), request);
        var response = new DescribeResourceGroupResponse { Status = NextStatus() };
        response.ResourceGroup = new Milvus.Client.Grpc.ResourceGroup { Name = request.ResourceGroup };
        return Task.FromResult(response);
    }

    // ---- Utility ----

    public override Task<ManualCompactionResponse> ManualCompaction(ManualCompactionRequest request, ServerCallContext context)
    {
        Record(nameof(ManualCompaction), request);
        LastRequestDbName = context.RequestHeaders.GetValue("dbname");
        return Task.FromResult(new ManualCompactionResponse { Status = NextStatus(), CompactionID = 42 });
    }

    public override Task<GetCompactionStateResponse> GetCompactionState(GetCompactionStateRequest request, ServerCallContext context)
    {
        Record(nameof(GetCompactionState), request);
        return Task.FromResult(new GetCompactionStateResponse { Status = NextStatus(), State = Grpc.CompactionState.Completed });
    }

    public override Task<GetCompactionPlansResponse> GetCompactionStateWithPlans(
        GetCompactionPlansRequest request, ServerCallContext context)
    {
        Record(nameof(GetCompactionStateWithPlans), request);
        return Task.FromResult(new GetCompactionPlansResponse { Status = NextStatus(), State = Grpc.CompactionState.Completed });
    }

    public override Task<FlushResponse> Flush(FlushRequest request, ServerCallContext context)
    {
        Record(nameof(Flush), request);
        var response = new FlushResponse { Status = NextStatus() };
        foreach (string collectionName in request.CollectionNames)
        {
            response.CollSegIDs[collectionName] = new LongArray();
            response.CollSegIDs[collectionName].Data.Add(1);
            response.CollSegIDs[collectionName].Data.Add(2);
        }

        return Task.FromResult(response);
    }

    public override Task<GetFlushStateResponse> GetFlushState(GetFlushStateRequest request, ServerCallContext context)
    {
        Record(nameof(GetFlushState), request);
        return Task.FromResult(new GetFlushStateResponse { Status = NextStatus(), Flushed = true });
    }

    public override Task<GetPersistentSegmentInfoResponse> GetPersistentSegmentInfo(
        GetPersistentSegmentInfoRequest request, ServerCallContext context)
    {
        Record(nameof(GetPersistentSegmentInfo), request);
        return Task.FromResult(new GetPersistentSegmentInfoResponse { Status = NextStatus() });
    }

    public override Task<FlushAllResponse> FlushAll(FlushAllRequest request, ServerCallContext context)
    {
        Record(nameof(FlushAll), request);
        return Task.FromResult(new FlushAllResponse { Status = NextStatus() });
    }

    public override Task<GetFlushAllStateResponse> GetFlushAllState(GetFlushAllStateRequest request, ServerCallContext context)
    {
        Record(nameof(GetFlushAllState), request);
        return Task.FromResult(new GetFlushAllStateResponse { Status = NextStatus(), Flushed = true });
    }

    public override Task<GetQuerySegmentInfoResponse> GetQuerySegmentInfo(
        GetQuerySegmentInfoRequest request, ServerCallContext context)
    {
        Record(nameof(GetQuerySegmentInfo), request);
        return Task.FromResult(new GetQuerySegmentInfoResponse { Status = NextStatus() });
    }

    public override Task<RunAnalyzerResponse> RunAnalyzer(RunAnalyzerRequest request, ServerCallContext context)
    {
        Record(nameof(RunAnalyzer), request);
        return Task.FromResult(new RunAnalyzerResponse { Status = NextStatus() });
    }

    public override Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
    {
        Record(nameof(GetMetrics), request);
        return Task.FromResult(new GetMetricsResponse { Status = NextStatus() });
    }

    public override Task<GetReplicateInfoResponse> GetReplicateInfo(GetReplicateInfoRequest request, ServerCallContext context)
    {
        Record(nameof(GetReplicateInfo), request);
        return Task.FromResult(new GetReplicateInfoResponse());
    }

    public override Task<Grpc.Status> UpdateReplicateConfiguration(
        UpdateReplicateConfigurationRequest request, ServerCallContext context)
    {
        Record(nameof(UpdateReplicateConfiguration), request);
        return Task.FromResult(NextStatus());
    }

    public override Task<GetReplicateConfigurationResponse> GetReplicateConfiguration(
        GetReplicateConfigurationRequest request, ServerCallContext context)
    {
        Record(nameof(GetReplicateConfiguration), request);
        return Task.FromResult(new GetReplicateConfigurationResponse { Status = NextStatus() });
    }

    /// <summary>
    /// When set, <c>DumpMessages</c> aborts the stream with a CANCELLED status. Combined with a caller token
    /// that is not cancelled this simulates a server-initiated abort, which must surface as a MilvusException
    /// (not an OperationCanceledException).
    /// </summary>
    public bool DumpMessagesCancel { get; set; }

    public override Task DumpMessages(DumpMessagesRequest request, IServerStreamWriter<DumpMessagesResponse> responseStream, ServerCallContext context)
    {
        Record(nameof(DumpMessages), request);
        if (DumpMessagesCancel)
        {
            throw new RpcException(new global::Grpc.Core.Status(StatusCode.Cancelled, "server aborted"));
        }

        return Task.CompletedTask;
    }

    private static readonly Milvus.Client.Grpc.Status OkStatus = new() { Code = 0, Reason = "Success" };

    private static bool IsVectorType(Milvus.Client.V2.Types.DataType dataType)
        => dataType is Milvus.Client.V2.Types.DataType.FloatVector or Milvus.Client.V2.Types.DataType.Float16Vector
            or Milvus.Client.V2.Types.DataType.BFloat16Vector or Milvus.Client.V2.Types.DataType.BinaryVector
            or Milvus.Client.V2.Types.DataType.Int8Vector or Milvus.Client.V2.Types.DataType.SparseFloatVector;
}

/// <summary>
/// Hosts a <see cref="MockMilvusService" /> in an in-process TestServer and hands out clients bound to it.
/// </summary>
internal sealed class MockMilvusServer : IDisposable
{
    private readonly IHost _host;

    public MockMilvusServer()
    {
        // Use a unique logical address per server so the process-wide SchemaCache/CollectionTsCache are keyed
        // by this server's Uri and naturally isolated from other MockMilvusServer instances (all of which
        // otherwise share TestServer's default http://localhost/ base address).
        string uniquePath = Guid.NewGuid().ToString("N");

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseUrls($"http://localhost/{uniquePath}/")
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<MockMilvusService>();
                        services.AddGrpc();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapGrpcService<MockMilvusService>());
                    });
            })
            .Start();

        Service = _host.Services.GetRequiredService<MockMilvusService>();
    }

    /// <summary>
    /// The mock service whose state the tests control.
    /// </summary>
    public MockMilvusService Service { get; }

    /// <summary>
    /// The URI of the mock server.
    /// </summary>
    public string Uri => _host.GetTestServer().BaseAddress.ToString();

    /// <summary>
    /// Channel options that route to the in-process mock server.
    /// </summary>
    public GrpcChannelOptions ChannelOptions
        => new() { HttpHandler = _host.GetTestServer().CreateHandler() };

    /// <summary>
    /// Creates a <see cref="MilvusClientV2" /> connected to the mock server.
    /// </summary>
    public MilvusClientV2 CreateClient()
        => new(new ConnectConfig { Uri = Uri, ChannelOptions = ChannelOptions });

    public void Dispose() => _host.Dispose();
}
