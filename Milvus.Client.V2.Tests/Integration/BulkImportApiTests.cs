using System.Text.Json;
using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

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
        MilvusBulkImport.HttpMessageHandler = new CapturingHandler(
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
            using JsonDocument response = await MilvusBulkImport.GetImportJobProgressAsync(
                "http://localhost:19530", "job-99", "db1", "key", TestContext.Current.CancellationToken);
            Assert.Equal("/v2/vectordb/jobs/import/get_progress", capturedPath);
            Assert.Contains("\"jobID\":\"job-99\"", capturedBody);
            Assert.Equal("db1", capturedDbNameHeader);
            Assert.Equal("COMPLETED", response.RootElement.GetProperty("data").GetProperty("state").GetString());
        }
        finally
        {
            MilvusBulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_sends_db_name_header()
    {
        string? capturedDbNameHeader = null;
        MilvusBulkImport.HttpMessageHandler = new CapturingHandler(
            req =>
            {
                capturedDbNameHeader = req.Headers.TryGetValues("DB-Name", out IEnumerable<string>? values)
                    ? string.Join(",", values)
                    : null;
            },
            "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            using JsonDocument response = await MilvusBulkImport.CreateImportJobsAsync(
                "http://localhost:19530", "coll", new[] { "file1" },
                "db1", "key", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("db1", capturedDbNameHeader);
        }
        finally
        {
            MilvusBulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_throws_on_business_error_in_http_200_body()
    {
        MilvusBulkImport.HttpMessageHandler = new CapturingHandler(
            _ => { },
            "{\"code\":90001,\"message\":\"collection not found\"}");
        try
        {
            await Assert.ThrowsAsync<MilvusException>(() =>
                MilvusBulkImport.CreateImportJobsAsync(
                    "http://localhost:19530", "coll", new[] { "file1" },
                    "default", "key", cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            MilvusBulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_sends_rest_request()
    {
        string? capturedPath = null;
        string? capturedBody = null;
        MilvusBulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedPath = req.RequestUri!.AbsolutePath; capturedBody = req.Content!.ReadAsStringAsync().Result; },
            "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            var options = new Dictionary<string, string> { ["format"] = "json" };
            using JsonDocument response = await MilvusBulkImport.CreateImportJobsAsync(
                "http://localhost:19530", "coll", new[] { "file1", "file2" },
                "default", "key", "p1", options, TestContext.Current.CancellationToken);
            Assert.Equal("/v2/vectordb/jobs/import/create", capturedPath);
            Assert.Contains("\"collectionName\":\"coll\"", capturedBody);
            Assert.Contains("\"partitionName\":\"p1\"", capturedBody);
            Assert.Contains("\"format\":\"json\"", capturedBody);
            Assert.Contains("\"files\":[[\"file1\",\"file2\"]]", capturedBody);
            Assert.Equal("job-1", response.RootElement.GetProperty("data").GetProperty("jobId").GetString());
        }
        finally
        {
            MilvusBulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task Import_rejects_empty_files()
    {
        MilvusBulkImport.HttpMessageHandler = new CapturingHandler(
            _ => { }, "{\"code\":0,\"data\":{\"jobId\":\"job-1\"}}");
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                MilvusBulkImport.CreateImportJobsAsync(
                    "http://localhost:19530", "coll", Array.Empty<string>(),
                    cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            MilvusBulkImport.HttpMessageHandler = null;
        }
    }

    [Fact]
    public async Task ListImportJobs_sends_rest_request()
    {
        string? capturedPath = null;
        string? capturedBody = null;
        MilvusBulkImport.HttpMessageHandler = new CapturingHandler(
            req => { capturedPath = req.RequestUri!.AbsolutePath; capturedBody = req.Content!.ReadAsStringAsync().Result; },
            "{\"code\":0,\"data\":{\"records\":[]}}");
        try
        {
            using JsonDocument response = await MilvusBulkImport.ListImportJobsAsync(
                "http://localhost:19530", "coll", "default", "key", TestContext.Current.CancellationToken);
            Assert.Equal("/v2/vectordb/jobs/import/list", capturedPath);
            Assert.Contains("\"collectionName\":\"coll\"", capturedBody);
            Assert.Equal(0, response.RootElement.GetProperty("data").GetProperty("records").GetArrayLength());
        }
        finally
        {
            MilvusBulkImport.HttpMessageHandler = null;
        }
    }
}

internal sealed class CapturingHandler : HttpMessageHandler
{
    private readonly Action<HttpRequestMessage> _onRequest;
    private readonly string _response;

    public CapturingHandler(Action<HttpRequestMessage> onRequest, string response)
    {
        _onRequest = onRequest;
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _onRequest(request);
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
