using System.Net.Http;

namespace IO.Milvus.ApiSchema;

internal interface IRestRequest
{
    HttpRequestMessage BuildRest();
}
