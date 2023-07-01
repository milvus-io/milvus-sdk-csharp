using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IO.Milvus.Client.REST;

internal static class HttpRequest
{
    private static readonly HttpMethod s_patchMethod = new HttpMethod("PATCH");

    public static HttpRequestMessage CreateGetRequest(string url, object payload = null) =>
        CreateRequest(HttpMethod.Get, url, payload);

    public static HttpRequestMessage CreatePostRequest(string url, object payload = null) =>
        CreateRequest(HttpMethod.Post, url, payload);

    public static HttpRequestMessage CreatePatchRequest(string url, object payload = null) =>
        CreateRequest(s_patchMethod, url, payload);

    public static HttpRequestMessage CreateDeleteRequest(string url,object payload = null) =>
        CreateRequest(HttpMethod.Delete, url, payload);

    public static HttpRequestMessage CreateRequest(HttpMethod method, string url, object payload = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (payload is not null)
        {
            byte[] utf8Bytes = payload is string s ?
                Encoding.UTF8.GetBytes(s) :
                JsonSerializer.SerializeToUtf8Bytes(payload);
            
            request.Content = new ByteArrayContent(utf8Bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        }

        return request;
    }
}