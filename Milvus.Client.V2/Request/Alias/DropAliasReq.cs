using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Aliases;

/// <summary>
/// Represents a request to drop an alias.
/// </summary>
public sealed class DropAliasReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the alias to drop.
    /// </summary>
    public string Alias { get; set; } = "";
    internal Grpc.DropAliasRequest ToGrpcDropAliasRequest()
    {
        Verify.NotNullOrWhiteSpace(Alias);
        var request = new Grpc.DropAliasRequest { Alias = Alias };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
