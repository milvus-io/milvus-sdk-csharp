using IO.Milvus.Client.REST;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal class GetCompactionPlansRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetCompactionPlansRequest>
{
    [JsonPropertyName("compactionID")]
    public long CompactionId { get; set; }

    public static GetCompactionPlansRequest Create(long compactionId)
    {
        return new GetCompactionPlansRequest(compactionId);
    }

    public Grpc.GetCompactionPlansRequest BuildGrpc()
    {
        Validate();

        return new Grpc.GetCompactionPlansRequest()
        {
            CompactionID = CompactionId
        };
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/compaction/plans",
            payload: this
            );
    }

    public void Validate()
    { }

    #region Private ======================================================
    private GetCompactionPlansRequest(long compactionId)
    {
        CompactionId = compactionId;
    }
    #endregion
}