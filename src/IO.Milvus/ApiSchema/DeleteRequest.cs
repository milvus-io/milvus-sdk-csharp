using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete rows of data entities from a collection by given expression
/// </summary>
internal sealed class DeleteRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Expr
    /// </summary>
    [JsonPropertyName("expr")]
    public string Expr { get; set; }

    /// <summary>
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static DeleteRequest Create(string collectionName, string expr, string dbName)
    {
        return new DeleteRequest(collectionName, expr, dbName);
    }

    public DeleteRequest WithPartitionName(string partitionName)
    {
        if (!string.IsNullOrEmpty(PartitionName))
            PartitionName = partitionName;

        return this;
    }

    public Grpc.DeleteRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.DeleteRequest()
        {
            CollectionName = this.CollectionName,
            Expr = this.Expr,
            DbName = this.DbName
        };

        if (!string.IsNullOrEmpty(PartitionName))
        {
            request.PartitionName = this.PartitionName;
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/entities",
            payload: this);
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(Expr);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region Private ===========================================================
    private DeleteRequest(string collectionName, string expr, string dbName)
    {
        CollectionName = collectionName;
        Expr = expr;
        DbName = dbName;
    }
    #endregion
}