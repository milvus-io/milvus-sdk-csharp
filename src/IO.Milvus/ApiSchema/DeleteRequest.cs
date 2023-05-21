using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete rows of data entities from a collection by given expression
/// </summary>
internal class DeleteRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.DeleteRequest>
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

    public static DeleteRequest Create(string collectionName, string expr)
    {
        return new DeleteRequest(collectionName, expr);
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
            payload:this);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.ArgNotNullOrEmpty(Expr, "Expr cannot be null or empty");
    }

    #region Private ===========================================================
    private DeleteRequest(string collectionName, string expr)
    {
        CollectionName = collectionName;
        Expr = expr;
    }
    #endregion
}