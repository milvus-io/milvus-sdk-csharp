using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// do a explicit record query by given expression. 
/// For example when you want to query by primary key.
/// </summary>
internal sealed class QueryRequest
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
    public IDictionary<string, string> QueryParams = new Dictionary<string, string>();

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static QueryRequest Create(string collectionName, string expr, string dbName)
    {
        return new QueryRequest(collectionName, expr, dbName);
    }

    public Grpc.QueryRequest BuildGrpc()
    {
        Validate();

        var request = new Grpc.QueryRequest()
        {
            CollectionName = CollectionName,
            Expr = Expr,
            GuaranteeTimestamp = (ulong)GuaranteeTimestamp,
            TravelTimestamp = (ulong)TravelTimestamp,
            DbName = DbName
        };

        request.OutputFields.AddRange(OutFields);
        if (PartitionNames?.Count > 0)
        {
            request.PartitionNames.AddRange(PartitionNames);
        }

        if (Offset > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair()
            {
                Key = "offset",
                Value = Offset.ToString(CultureInfo.InvariantCulture)
            });
        }
        if (Limit > 0)
        {
            request.QueryParams.Add(new Grpc.KeyValuePair()
            {
                Key = "limit",
                Value = Limit.ToString(CultureInfo.InvariantCulture)
            });
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        if (Offset > 0)
        {
            QueryParams.Add("offset", Offset.ToString(CultureInfo.InvariantCulture));
        }
        if (Limit > 0)
        {
            QueryParams.Add("limit", Limit.ToString(CultureInfo.InvariantCulture));
        }

        var request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/query",
            payload: this);

        return request;
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(OutFields);
        Verify.NotNullOrWhiteSpace(Expr);
        Verify.GreaterThanOrEqualTo(GuaranteeTimestamp, 0);
        Verify.GreaterThanOrEqualTo(TravelTimestamp, 0);
        Verify.GreaterThanOrEqualTo(Offset, 0);
        Verify.GreaterThanOrEqualTo(Limit, 0);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    internal QueryRequest WithOutputFields(IList<string> outputFields)
    {
        OutFields = outputFields;
        return this;
    }

    internal QueryRequest WithOffset(long offset)
    {
        Offset = offset;
        return this;
    }

    internal QueryRequest WithLimit(long limit)
    {
        Limit = limit;
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
        GracefulTime = gracefulTime;
        return this;
    }

    internal QueryRequest WithTravelTimestamp(long travelTimestamp)
    {
        TravelTimestamp = (int)travelTimestamp;
        return this;
    }

    internal QueryRequest WithConsistencyLevel(MilvusConsistencyLevel consistencyLevel)
    {
        ConsistencyLevel = consistencyLevel;
        return this;
    }

    #region Private ====================================================
    private QueryRequest(string collectionName, string expr, string dbName)
    {
        CollectionName = collectionName;
        Expr = expr;
        DbName = dbName;
    }
    #endregion
}