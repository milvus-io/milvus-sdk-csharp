using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
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
    public long TravelTimestamp { get; set; }

    [JsonPropertyName("graceful_time")]
    public long GracefulTime { get; private set; }

    [JsonPropertyName("consistency_level")]
    public MilvusConsistencyLevel ConsistencyLevel { get; private set; }

    [JsonIgnore]
    public long Offset { get; private set; }

    [JsonIgnore]
    public long Limit { get; private set; }

    [JsonPropertyName("query_params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string,string> QueryParams = new Dictionary<string, string>();

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static QueryRequest Create(string collectionName,string expr,string dbName)
    {
        return new QueryRequest(collectionName, expr, dbName);
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
            DbName = this.DbName
        };

        request.OutputFields.AddRange(OutFields);
        if (PartitionNames?.Any() == true)
        {
            request.PartitionNames.AddRange(PartitionNames);
        }

        if (Offset > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair()
            {
                Key = "offset",
                Value = Offset.ToString()
            });
        }
        if (Limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair()
            {
                Key = "limit",
                Value = Limit.ToString()
            });
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        if (Offset > 0)
        {
            QueryParams.Add("offset",Offset.ToString());
        }
        if (Limit > 0)
        {
            QueryParams.Add("limit",Limit.ToString());
        }

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
        Verify.True(this.GuaranteeTimestamp >= 0, "GuaranteeTimestamp must be greater than 0");
        Verify.True(this.TravelTimestamp >= 0, "TravelTimestamp must be greater than 0");
        Verify.True(this.Offset >= 0, "Offset must be greater than 0");
        Verify.True(this.Limit >= 0, "Limit must be greater than 0");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    internal QueryRequest WithOutputFields(IList<string> outputFields)
    {
        this.OutFields = outputFields;
        return this;
    }

    internal QueryRequest WithOffset(long offset)
    {
        this.Offset = offset;
        return this;
    }

    internal QueryRequest WithLimit(long limit)
    {
        this.Limit = limit;
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

    internal QueryRequest WithGracefulTimestamp(long gracefulTime)
    {
        this.GracefulTime = gracefulTime;
        return this;
    }

    internal QueryRequest WithTravelTimestamp(long travelTimestamp)
    {
        TravelTimestamp = (int)travelTimestamp;
        return this;
    }

    internal QueryRequest WithConsistencyLevel(MilvusConsistencyLevel consistencyLevel)
    {
        this.ConsistencyLevel = consistencyLevel;
        return this;
    }

    #region Private ====================================================
    private QueryRequest(string collectionName, string expr, string dbName)
    {
        this.CollectionName = collectionName;
        this.Expr = expr;
        this.DbName = dbName;
    }
    #endregion
}