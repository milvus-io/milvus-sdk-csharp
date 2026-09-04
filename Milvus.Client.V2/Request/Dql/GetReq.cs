using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to fetch rows by primary key.
/// </summary>
public sealed class GetReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The primary key values to fetch.
    /// </summary>
    public IReadOnlyList<object> Ids { get; set; } = Array.Empty<object>();

    /// <summary>
    /// The fields to return. Empty means all fields.
    /// </summary>
    public IReadOnlyList<string>? OutputFields { get; set; }

    internal Grpc.QueryRequest ToGrpcQueryRequest(string primaryKeyField)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(Ids);

        string idList = string.Join(", ", Ids.Select(FormatId));
        var request = new Grpc.QueryRequest
        {
            CollectionName = CollectionName,
            Expr = $"{primaryKeyField} in [{idList}]"
        };

        if (OutputFields is { Count: > 0 })
        {
            request.OutputFields.AddRange(OutputFields);
        }

        return request;
    }

    private static string FormatId(object id) => id is string s ? $"\"{EscapeString(s)}\"" : id.ToString()!;

    // Escape backslashes and double quotes so string primary keys containing them (or other special
    // characters) still produce a valid boolean expression and cannot be used for expression injection.
    private static string EscapeString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
