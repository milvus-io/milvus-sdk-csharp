using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete an Alias
/// </summary>
internal sealed class DropAliasRequest
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static DropAliasRequest Create(string alias, string dbName)
    {
        return new DropAliasRequest(alias,dbName);
    }

    public Grpc.DropAliasRequest BuildGrpc()
    {
        return new Grpc.DropAliasRequest()
        {            
            Alias = Alias,
            DbName = DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/alias",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(Alias, "Alias cannot be null or empty");
    }

    #region Private ============================================================================
    public DropAliasRequest(string alias, string dbName)
    {
        Alias = alias;
        DbName = dbName;
    }
    #endregion
}