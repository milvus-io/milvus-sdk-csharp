using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class InsertResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }


}
