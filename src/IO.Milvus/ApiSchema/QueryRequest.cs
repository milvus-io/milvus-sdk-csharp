using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// do a explicit record query by given expression. 
/// For example when you want to query by primary key.
/// </summary>
internal sealed class QueryRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.QueryRequest>
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// expr
    /// </summary>
    [JsonPropertyName("expr")]
    public string Expr { get; set; }

    [JsonPropertyName("output_fields")]
    public IList<string> OutFields { get; set; }

    /// <summary>
    /// Guarantee timestamp
    /// </summary>
    [JsonPropertyName("guarantee_timestamp")]
    public long GuaranteeTimestamp { get; set; }

    /// <summary>
    /// Partition names
    /// </summary>
    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    /// <summary>
    /// Travel timestamp
    /// </summary>
    [JsonPropertyName("travel_timestamp")]
    public int TravelTimestamp { get; set; }

    public static QueryRequest Create(string collectionName,string expr)
    {
        return new QueryRequest(collectionName, expr);
    }

    public Grpc.QueryRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.QueryRequest()
        {
            CollectionName = this.CollectionName,
            Expr = this.Expr,
            GuaranteeTimestamp = (ulong)this.GuaranteeTimestamp,
            TravelTimestamp = (ulong)this.TravelTimestamp,            
        };

        request.OutputFields.AddRange(OutFields);
        if (PartitionNames?.Any() == true)
        {
            request.PartitionNames.AddRange(PartitionNames);
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        var request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/query",
            payload: this);

        return request;
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(this.CollectionName, "Milvus collection name cannot be null or empty");
        Verify.True(this.OutFields?.Any() == true, "OutputFields cannot be null or empty");
        Verify.ArgNotNullOrEmpty(this.Expr, "Expr cannot be null or empty");
    }

    internal QueryRequest WithOutputFields(IList<string> outputFields)
    {
        this.OutFields = outputFields;
        return this;
    }

    internal QueryRequest WithPartitionNames(IList<string> partitionNames)
    {
        PartitionNames = partitionNames;
        return this;
    }

    internal QueryRequest WithGuaranteeTimestamp(long guarantee_timestamp)
    {
        GuaranteeTimestamp = guarantee_timestamp;
        return this;
    }

    #region Private ====================================================
    private QueryRequest(string collectionName, string expr)
    {
        this.CollectionName = collectionName;
        this.Expr = expr;
    }
    #endregion
}