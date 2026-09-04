using Grpc.Net.Client;
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

    private Milvus.Client.Grpc.Status NextStatus()
    {
        TotalCalls++;
        if (FailNextCalls > 0)
        {
            FailNextCalls--;
            return FailureStatus ?? new Milvus.Client.Grpc.Status { Code = (int)MilvusErrorCode.RateLimit, Reason = "rate limited" };
        }

        return FailureStatus ?? OkStatus;
    }

    public string? LastCreatedCollectionName { get; private set; }
    public string? LastDroppedCollectionName { get; private set; }
    public string? LastCheckedCollectionName { get; private set; }
    public Milvus.Client.Grpc.ClientInfo? LastConnectClientInfo { get; private set; }

    public override Task<Milvus.Client.Grpc.Status> CreateCollection(
        CreateCollectionRequest request, ServerCallContext context)
    {
        LastCreatedCollectionName = request.CollectionName;
        return Task.FromResult(NextStatus());
    }

    public override Task<Milvus.Client.Grpc.Status> DropCollection(
        DropCollectionRequest request, ServerCallContext context)
    {
        LastDroppedCollectionName = request.CollectionName;
        return Task.FromResult(NextStatus());
    }

    public override Task<BoolResponse> HasCollection(HasCollectionRequest request, ServerCallContext context)
    {
        LastCheckedCollectionName = request.CollectionName;
        return Task.FromResult(new BoolResponse
        {
            Status = NextStatus(),
            Value = HasCollectionResult
        });
    }

    public override Task<ShowCollectionsResponse> ShowCollections(
        ShowCollectionsRequest request, ServerCallContext context)
    {
        var response = new ShowCollectionsResponse { Status = FailureStatus ?? OkStatus };
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
        var response = new DescribeCollectionResponse { Status = NextStatus() };
        if (DescribeSchema is not null)
        {
            response.Schema = new Grpc.CollectionSchema { Name = DescribeSchema.Name ?? request.CollectionName };
            foreach (Milvus.Client.V2.Types.FieldSchema field in DescribeSchema.Fields)
            {
                var grpcField = new Grpc.FieldSchema
                {
                    Name = field.Name,
                    DataType = (Grpc.DataType)(int)field.DataType,
                    IsPrimaryKey = field.IsPrimaryKey
                };
                response.Schema.Fields.Add(grpcField);
            }
        }
        return Task.FromResult(response);
    }

    public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
    {
        LastConnectClientInfo = request.ClientInfo;
        var response = new ConnectResponse { Status = FailureStatus ?? OkStatus };
        response.ServerInfo = new ServerInfo { BuildTags = ServerVersion };
        return Task.FromResult(response);
    }

    public override Task<GetVersionResponse> GetVersion(GetVersionRequest request, ServerCallContext context)
        => Task.FromResult(new GetVersionResponse { Status = FailureStatus ?? OkStatus, Version = ServerVersion });

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
    public string? LastDeletedCollection { get; private set; }
    public string? LastDeleteExpression { get; private set; }

    public override Task<MutationResult> Insert(InsertRequest request, ServerCallContext context)
    {
        LastInsertedCollection = request.CollectionName;
        LastInsertedRows = (int)request.NumRows;
        var result = new MutationResult { Status = NextStatus(), Timestamp = NextMutationTimestamp };
        result.InsertCnt = request.NumRows;
        return Task.FromResult(result);
    }

    public override Task<MutationResult> Upsert(UpsertRequest request, ServerCallContext context)
    {
        LastInsertedCollection = request.CollectionName;
        LastInsertedRows = (int)request.NumRows;
        var result = new MutationResult { Status = NextStatus(), Timestamp = NextMutationTimestamp };
        result.UpsertCnt = request.NumRows;
        return Task.FromResult(result);
    }

    public override Task<MutationResult> Delete(DeleteRequest request, ServerCallContext context)
    {
        LastDeletedCollection = request.CollectionName;
        LastDeleteExpression = request.Expr;
        var result = new MutationResult { Status = NextStatus(), Timestamp = NextMutationTimestamp };
        result.DeleteCnt = 1;
        return Task.FromResult(result);
    }

    public string? LastSearchedCollection { get; private set; }
    public int LastSearchTopK { get; private set; }
    public ulong? LastSearchGuaranteeTimestamp { get; private set; }
    public IReadOnlyList<KeyValuePair<string, string>> LastSearchParams { get; private set; } = [];

    public override Task<SearchResults> Search(SearchRequest request, ServerCallContext context)
    {
        LastSearchedCollection = request.CollectionName;
        LastSearchTopK = int.Parse(request.SearchParams.Single(p => p.Key == "topk").Value, System.Globalization.CultureInfo.InvariantCulture);
        LastSearchGuaranteeTimestamp = request.GuaranteeTimestamp;
        LastSearchParams = request.SearchParams.Select(p => new KeyValuePair<string, string>(p.Key, p.Value)).ToList();

        var results = new SearchResults
        {
            Status = NextStatus(),
            CollectionName = request.CollectionName,
            Results = new SearchResultData { NumQueries = 1, TopK = LastSearchTopK }
        };
        results.Results.Scores.AddRange(Enumerable.Repeat(0.5f, LastSearchTopK));
        results.Results.Ids = new Milvus.Client.Grpc.IDs();
        results.Results.Ids.IntId = new Milvus.Client.Grpc.LongArray();
        results.Results.Ids.IntId.Data.AddRange(Enumerable.Range(0, LastSearchTopK).Select(i => (long)(i + 1)));
        return Task.FromResult(results);
    }

    public string? LastQueriedCollection { get; private set; }
    public string? LastQueryExpression { get; private set; }
    public ulong? LastQueryGuaranteeTimestamp { get; private set; }

    public override Task<QueryResults> Query(QueryRequest request, ServerCallContext context)
    {
        LastQueriedCollection = request.CollectionName;
        LastQueryExpression = request.Expr;
        LastQueryGuaranteeTimestamp = request.GuaranteeTimestamp;

        var results = new QueryResults { Status = NextStatus(), CollectionName = request.CollectionName };
        var idField = new Grpc.FieldData { FieldName = "id", Type = Grpc.DataType.Int64 };
        idField.Scalars = new Grpc.ScalarField();
        idField.Scalars.LongData = new Grpc.LongArray();
        idField.Scalars.LongData.Data.Add(1);
        results.FieldsData.Add(idField);
        return Task.FromResult(results);
    }

    private static readonly Milvus.Client.Grpc.Status OkStatus = new() { Code = 0, Reason = "Success" };
}

/// <summary>
/// Hosts a <see cref="MockMilvusService" /> in an in-process TestServer and hands out clients bound to it.
/// </summary>
internal sealed class MockMilvusServer : IDisposable
{
    private readonly IHost _host;

    public MockMilvusServer()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
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
