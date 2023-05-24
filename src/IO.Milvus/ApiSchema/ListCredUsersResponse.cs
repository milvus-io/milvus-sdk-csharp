using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal class ListCredUsersResponse
{
    [JsonPropertyName("usernames")]
    public IList<string> Usernames { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}
