using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetMetricsRequest
{
    [JsonPropertyName("request")]
    public string Request { get;set; }

    public static GetMetricsRequest Create(string request)
    {
        return new GetMetricsRequest(request);
    }

    public Grpc.GetMetricsRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetMetricsRequest()
        {
            Request = this.Request,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/metrics",
            payload: this
        );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(Request, "Request cannot be null or empty.");
    }

    #region Private ==========================================
    private GetMetricsRequest(string request)
    {
        this.Request = request;
    }
    #endregion
}