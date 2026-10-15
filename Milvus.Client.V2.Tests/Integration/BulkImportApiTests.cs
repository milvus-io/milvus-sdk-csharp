using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class BulkImportApiTests
{
    [Fact]
    public async Task GetImportProgress_sends_rest_request()
    {
        string? capturedPath = null;
        string? capturedBody = null;
        string? capturedDbNameHeader = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req =>
            {
                capturedPath = req.RequestUri!.AbsolutePath;
                capturedBody = req.Content!.ReadAsStringAsync().Result;
                capturedDbNameHeader = req.Headers.TryGetValues("DB-Name", out IEnumerable<string>? values)
                    ? string.Join(",", values)
                    : null;
            },
            "{\"code\":0,\"data\":{\"state\":\"COMPLETED\",\"importedRows\":10}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", ApiKey = "key" });
            using BulkImportResp response = await client.GetImportProgressAsync(
                new GetImportProgressRequest { DatabaseName = "db1", JobId = "job-99" },
                TestContext.Current.CancellationToken);
            Assert.Equal("/v2/vectordb/jobs/import/get_progress", capturedPath);
            Assert.Contains("\"jobID\":\"job-99\"", capturedBody);
            Assert.Equal("db1", capturedDbNameHeader);
            Assert.Equal("COMPLETED", response.State);
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_sends_db_name_header()
    {
        string? capturedDbNameHeader = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req =>
            {
                capturedDbNameHeader = req.Headers.TryGetValues("DB-Name", out IEnumerable<string>? values)
                    ? string.Join(",", values)
                    : null;
            },
            "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", ApiKey = "key" });
            using BulkImportResp response = await client.BulkImportAsync(
                new BulkImportRequest
                {
                    DatabaseName = "db1",
                    CollectionName = "coll",
                    Files = [new[] { "file1" }]
                },
                TestContext.Current.CancellationToken);
            Assert.Equal("db1", capturedDbNameHeader);
            Assert.Equal("job-1", response.JobId);
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_business_error_in_http_200_body()
    {
        BulkImport.HttpMessageHandler = new CapturingHandler(
            _ => { },
            "{\"code\":90001,\"message\":\"collection not found\"}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", ApiKey = "key" });
            await Assert.ThrowsAsync<MilvusException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest
                    {
                        DatabaseName = "default",
                        CollectionName = "coll",
                        Files = [new[] { "file1" }]
                    },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_sends_rest_request()
    {
        string? capturedPath = null;
        string? capturedBody = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedPath = req.RequestUri!.AbsolutePath; capturedBody = req.Content!.ReadAsStringAsync().Result; },
            "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", ApiKey = "key" });
            using BulkImportResp response = await client.BulkImportAsync(
                new BulkImportRequest
                {
                    DatabaseName = "default",
                    CollectionName = "coll",
                    PartitionName = "p1",
                    Files = [new[] { "file1", "file2" }],
                    Options = { ["format"] = "json" }
                },
                TestContext.Current.CancellationToken);
            Assert.Equal("/v2/vectordb/jobs/import/create", capturedPath);
            Assert.Contains("\"collectionName\":\"coll\"", capturedBody);
            Assert.Contains("\"partitionName\":\"p1\"", capturedBody);
            Assert.Contains("\"format\":\"json\"", capturedBody);
            Assert.Contains("\"files\":[[\"file1\",\"file2\"]]", capturedBody);
            Assert.Equal("job-1", response.JobId);
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_rejects_empty_files()
    {
        BulkImport.HttpMessageHandler = new CapturingHandler(
            _ => { }, "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            await Assert.ThrowsAsync<ArgumentException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest { CollectionName = "coll", Files = [] },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_rejects_multiple_sources()
    {
        BulkImport.HttpMessageHandler = new CapturingHandler(
            _ => { }, "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            await Assert.ThrowsAsync<ArgumentException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest
                    {
                        CollectionName = "coll",
                        Files = [new[] { "file1" }],
                        ObjectUrls = [new[] { "s3://bucket/obj" }]
                    },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_sends_bearer_authorization_header()
    {
        // The API key travels verbatim as an "Authorization: Bearer <key>" header (not base64(username:password));
        // assert it so a regression in the auth-wire format cannot pass undetected.
        string? capturedAuth = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedAuth = req.Headers.Authorization?.ToString(); },
            "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", ApiKey = "s3cret-key" });
            using BulkImportResp response = await client.BulkImportAsync(
                new BulkImportRequest { CollectionName = "coll", Files = [new[] { "file1" }] },
                TestContext.Current.CancellationToken);
            Assert.Equal("Bearer s3cret-key", capturedAuth);
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task ListImportJobs_sends_rest_request()
    {
        string? capturedPath = null;
        string? capturedBody = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedPath = req.RequestUri!.AbsolutePath; capturedBody = req.Content!.ReadAsStringAsync().Result; },
            "{\"code\":0,\"data\":{\"records\":[]}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", ApiKey = "key" });
            using BulkImportResp response = await client.ListImportJobsAsync(
                new ListImportJobsRequest { DatabaseName = "default", CollectionName = "coll" },
                TestContext.Current.CancellationToken);
            Assert.Equal("/v2/vectordb/jobs/import/list", capturedPath);
            Assert.Contains("\"collectionName\":\"coll\"", capturedBody);
            Assert.Equal(0, response.Data.GetProperty("records").GetArrayLength());
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_http_error_status()
    {
        BulkImport.HttpMessageHandler = new CapturingHandler(
            _ => { }, "oops", System.Net.HttpStatusCode.InternalServerError);
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            await Assert.ThrowsAsync<MilvusException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest { CollectionName = "coll", Files = [new[] { "file1" }] },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_transport_failure()
    {
        BulkImport.HttpMessageHandler = new ThrowingHandler(new HttpRequestException("connection reset"));
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            await Assert.ThrowsAsync<MilvusException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest { CollectionName = "coll", Files = [new[] { "file1" }] },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_timeout()
    {
        // A client-side timeout surfaces as TaskCanceledException (not the caller's cancellation); it must map
        // to MilvusException rather than leaking.
        BulkImport.HttpMessageHandler = new ThrowingHandler(new TaskCanceledException());
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530", Timeout = TimeSpan.FromSeconds(1) });
            await Assert.ThrowsAsync<MilvusException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest { CollectionName = "coll", Files = [new[] { "file1" }] },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_non_json_body()
    {
        BulkImport.HttpMessageHandler = new CapturingHandler(_ => { }, "<html>gateway error</html>");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            await Assert.ThrowsAsync<MilvusException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest { CollectionName = "coll", Files = [new[] { "file1" }] },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_non_object_json_body()
    {
        BulkImport.HttpMessageHandler = new CapturingHandler(_ => { }, "[]");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            await Assert.ThrowsAsync<MilvusException>(() =>
                client.BulkImportAsync(
                    new BulkImportRequest { CollectionName = "coll", Files = [new[] { "file1" }] },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task GetImportProgress_sends_cloud_fields()
    {
        string? capturedBody = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedBody = req.Content!.ReadAsStringAsync().Result; },
            "{\"code\":0,\"data\":{\"state\":\"IN_PROGRESS\",\"progress\":50,\"reason\":\"\"}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            using BulkImportResp response = await client.GetImportProgressAsync(
                new GetImportProgressRequest { JobId = "job-9", ClusterId = "c1", ProjectId = "p1", RegionId = "r1" },
                TestContext.Current.CancellationToken);
            Assert.Contains("\"jobID\":\"job-9\"", capturedBody);
            Assert.Contains("\"clusterId\":\"c1\"", capturedBody);
            Assert.Contains("\"projectId\":\"p1\"", capturedBody);
            Assert.Contains("\"regionId\":\"r1\"", capturedBody);
            Assert.Equal("IN_PROGRESS", response.State);
            Assert.Equal(50, response.Progress);
            Assert.Equal("", response.Reason);
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task ListImportJobs_sends_cloud_fields()
    {
        string? capturedBody = null;
        BulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedBody = req.Content!.ReadAsStringAsync().Result; },
            "{\"code\":0,\"data\":{\"records\":[]}}");
        try
        {
            using BulkImport client = new(new BulkImportConfig { Url = "http://localhost:19530" });
            using BulkImportResp response = await client.ListImportJobsAsync(
                new ListImportJobsRequest { DatabaseName = "db", CollectionName = "coll", ClusterId = "c1" },
                TestContext.Current.CancellationToken);
            Assert.Contains("\"clusterId\":\"c1\"", capturedBody);
        }
        finally
        {
            BulkImport.HttpMessageHandler = null;
        }
    }
}

internal sealed class CapturingHandler : HttpMessageHandler
{
    private readonly Action<HttpRequestMessage> _onRequest;
    private readonly string _response;
    private readonly System.Net.HttpStatusCode _statusCode;

    public CapturingHandler(Action<HttpRequestMessage> onRequest, string response,
        System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        _onRequest = onRequest;
        _response = response;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _onRequest(request);
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

internal sealed class ThrowingHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    public ThrowingHandler(Exception exception) => _exception = exception;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw _exception;
}
