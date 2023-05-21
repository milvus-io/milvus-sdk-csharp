using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using IO.Milvus.Param;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace IO.Milvus;

/// <summary>
/// Metric Type
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Invalid
    /// </summary>
    Invalid,
    
    /// <summary>
    /// L2
    /// </summary>
    L2,

    /// <summary>
    /// IP
    /// </summary>
    IP,

    /// <summary>
    /// Only supported for binary vectors.
    /// </summary>
    Hamming,

    /// <summary>
    /// Jaccard.
    /// </summary>
    Jaccard,

    /// <summary>
    /// Tanimoto.
    /// </summary>
    Tanimoto,

    /// <summary>
    /// Sub structure
    /// </summary>
    Substructure,

    /// <summary>
    /// Super structure.
    /// </summary>
    Superstructure,
}

/// <summary>
/// Search parameters.
/// </summary>
public class SearchParameters:
    IValidatable,
    IGrpcRequest<Grpc.SearchRequest>
{
    /// <summary>
    /// the consistency level used in the query. 
    /// If the consistency level is not specified, 
    /// the default level is ConsistencyLevelEnum.BOUNDED.
    /// </summary>
    public ConsistencyLevel ConsistencyLevel { get; private set; } = ConsistencyLevel.Bounded;

    /// <summary>
    /// The name list of output fields.
    /// </summary>
    public IList<string> OutputFields { get; private set; } = new List<string>();

    /// <summary>
    /// The name list of the partitions to query.
    /// </summary>
    /// <remarks>
    /// (Optional).
    /// </remarks>
    public IList<string> PartitionNames { get; private set; } = new List<string>();

    /// <summary>
    /// An absolute timestamp value.
    /// </summary>
    public DateTime? TravelTime { get; private set; }

    /// <summary>
    /// the expression to filter scalar fields before searching.
    /// </summary>
    /// <remarks>
    /// (Optional)
    /// </remarks>
    public string Expr { get; private set; }

    /// <summary>
    /// The name of the collection to query.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The target vector field by name. The field name cannot be empty or null.
    /// </summary>
    public string VectorFieldName { get; private set; }

    /// <summary>
    /// The topK value.
    /// </summary>
    public int TopK { get;private set; }

    /// <summary>
    /// Guarantee timestamp
    /// </summary>
    public long GuaranteeTimestamp { get; private set; } = Constant.GUARANTEE_EVENTUALLY_TS;

    /// <summary>
    /// Metric type of ANN searching.
    /// </summary>
    public MetricType MetricType { get; private set; }

    /// <summary>
    /// The decimal place of the returned results.
    /// </summary>
    public long RoundDecimal { get;private set; }

    /// <summary>
    /// Parameters
    /// </summary>
    public IDictionary<string,string> Parameters { get; private set; } = new Dictionary<string,string>();

    /// <summary>
    /// Milvus Vector
    /// </summary>
    public IList<Field> MilvusVectors { get; private set; } = new List<Field>();

    /// <summary>
    /// Ignore the growing segments to get best search performance. Default is False.
    /// </summary>
    public bool IgnoreGrowing { get; private set; } = false;

    /// <summary>
    /// Create a search parameters
    /// </summary>
    /// <returns><see cref="SearchParameters"/></returns>
    public static SearchParameters Create(string collectionName)
    {
        return new SearchParameters(collectionName);
    }

    /// <summary>
    /// Specifies the output scalar fields (Optional). 
    /// If the output fields are specified, 
    /// the QueryResults returned by query() will contains the values of these fields.
    /// </summary>
    /// <param name="outputFields">The name list of output fields.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SearchParameters WithOutputFields(IList<string> outputFields)
    {
        if (outputFields is null)
        {
            throw new ArgumentNullException(nameof(outputFields));
        }

        foreach (var outField in outputFields)
        {
            this.OutputFields.Add(outField);
        }

        return this;
    }

    /// <summary>
    /// Sets a partition name list to specify query scope.
    /// </summary>
    /// <remarks>
    /// (Optional)
    /// </remarks>
    /// <exception cref="ArgumentNullException"></exception>
    /// <param name="partitionNames">The name list of the partitions to query.</param>
    public SearchParameters WithPartitionNames(IList<string> partitionNames)
    {
        if (partitionNames is null)
        {
            throw new ArgumentNullException(nameof(partitionNames));
        }

        foreach (var partitionName in partitionNames)
        {
            this.PartitionNames.Add(partitionName);
        }

        return this;
    }

    /// <summary>
    /// Sets the expression to filter scalar fields before searching (Optional).
    /// </summary>
    /// <param name="expr">The expression used to filter scalar fields.</param>
    /// <exception cref="ArgumentException"></exception>
    public SearchParameters WithExpr(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr))
        {
            throw new ArgumentException($"\"{nameof(expr)}\" cannot be null or whitespace.");
        }

        this.Expr = expr; 
        return this;
    }

    /// <summary>
    /// Sets the consistency level used in the query. If the consistency level is not specified,
    /// the default level is ConsistencyLevelEnum.BOUNDED.
    /// </summary>
    /// <param name="consistencyLevel">The consistency level used in the query.</param>
    public SearchParameters WithConsistencyLevel(ConsistencyLevel consistencyLevel)
    {
        this.ConsistencyLevel = consistencyLevel;
        return this;
    }

    /// <summary>
    /// Specifies an absolute timestamp in a query to get results based on a data view at a specified point in time (Optional).
    /// </summary>
    /// <remarks>
    /// (Optional).
    /// The default value is 0, with which the server executes the query on a full data view. For more information please refer to Search with Time Travel.
    /// <see href="https://milvus.io/docs/v2.1.x/timetravel.md"/>
    /// </remarks>
    /// <param name="travelTimestamp"></param>
    public SearchParameters WithTravelTimestamp(DateTime? travelTimestamp)
    {
        this.TravelTime = travelTimestamp;
        return this;
    }

    /// <summary>
    /// Specifies an output scalar field.
    /// </summary>
    /// <remarks>
    /// (Optional)
    /// </remarks>
    /// <exception cref="ArgumentException"/>
    /// <param name="fieldName">The name of an output field</param>
    public SearchParameters AddOutField(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException($"\"{nameof(fieldName)}\" cannot be null or whitespace.");
        }

        if (!this.OutputFields.Contains(fieldName))
        {
            this.OutputFields.Add(fieldName);
        }

        return this;
    }

    /// <summary>
    /// Adds a partition to specify search scope.
    /// </summary>
    /// <remarks>
    /// (Optional).
    /// </remarks>
    /// <param name="partitionName">partition name.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public SearchParameters AddPartitionName(string partitionName)
    {
        if (string.IsNullOrWhiteSpace(partitionName))
        {
            throw new ArgumentException($"\"{nameof(partitionName)}\" cannot be null or whitespace.");
        }

        if (!this.PartitionNames.Contains(partitionName))
        {
            this.PartitionNames.Add(partitionName);
        }

        return this;
    }

    /// <summary>
    /// Sets metric type of ANN searching.
    /// </summary>
    /// <param name="metricType">metric type</param>
    public SearchParameters WithMetricType(MetricType metricType)
    {
        this.MetricType = metricType; 
        return this;
    }

    /// <summary>
    /// Specifies the decimal place of the returned results.
    /// </summary>
    /// <param name="decimal">How many digits after the decimal point.</param>
    /// <returns></returns>
    public SearchParameters WithRoundDecimal(long @decimal)
    {
        this.RoundDecimal = @decimal;
        return this;
    }

    /// <summary>
    /// <list type="bullet">
    /// <item>
    /// Instructs server to see insert/delete operations performed before a provided timestamp.
    /// If no such timestamp is specified, the server will wait for the latest operation to finish and search.
    /// </item>
    /// <item>
    /// Note: The timestamp is not an absolute timestamp, it is a hybrid value combined by UTC time and internal flags.
    /// We call it TSO, for more information please refer to: <see href="https://github.com/milvus-io/milvus/blob/master/docs/design_docs/milvus_hybrid_ts_en.md"/>
    /// Use an operation's TSO to set this parameter, the server will execute search after this operation is finished.
    /// </item>
    /// <item>
    /// Default value is <see cref="Constant.GUARANTEE_EVENTUALLY_TS"/> , server executes search immediately.
    /// </item> 
    /// </list>
    /// </summary>
    /// <remarks>
    /// (Optional).
    /// </remarks>
    /// <param name="guaranteeTimestamp"></param>
    /// <returns></returns>
    public SearchParameters WithGuaranteeTimestamp(long guaranteeTimestamp)
    {
        GuaranteeTimestamp = guaranteeTimestamp;
        return this;
    }

    /// <summary>
    /// Sets the target vector field by name. The field name cannot be empty or null.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The field name cannot be empty or null.
    /// </exception>
    /// <param name="vectorFieldName">A vector field name.</param>
    public SearchParameters WithVectorFieldName(string vectorFieldName)
    {
        if (string.IsNullOrWhiteSpace(vectorFieldName))
        {
            throw new ArgumentException($"\"{nameof(vectorFieldName)}\" cannot be null or empty.");
        }

        VectorFieldName = vectorFieldName;
        return this;
    }

    /// <summary>
    /// Sets the topK value of ANN search.
    /// </summary>
    /// <remarks>
    /// The available range is [1, 16384].
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The available range is [1, 16384].
    /// </exception>
    /// <param name="topK">The topK value.</param>
    /// <returns></returns>
    public SearchParameters WithTopK(int topK)
    {
        if (topK < 1 || topK > 16384)
        {
            throw new ArgumentOutOfRangeException($"The available range is [1, 16384].");
        }

        TopK = topK;
        return this;
    }

    /// <summary>
    /// Add search parameter
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public SearchParameters WithParameter(string key,string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"\"{nameof(key)}\" cannot be null or empty");
        }

        this.Parameters[key] = value;
        return this;
    }

    /// <summary>
    /// Ignore the growing segments to get best search performance. Default is False.
    /// For the user case that don't require data visibility.
    /// </summary>
    /// <param name="ignoreGrowing">ignoreGrowing true ignore, Boolean.FALSE is not</param>
    /// <returns></returns>
    public SearchParameters WithIgnoreGrowing(bool ignoreGrowing)
    {
        IgnoreGrowing = ignoreGrowing;
        return this;
    }

    /// <summary>
    /// Build a grpc request
    /// </summary>
    /// <returns></returns>
    public Grpc.SearchRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.SearchRequest()
        {
            CollectionName = this.CollectionName,
            TravelTimestamp = this.TravelTime == null ? 0 : (ulong)this.TravelTime.Value.ToUtcTimestamp(),
        };

        if (this.PartitionNames?.Any() == true)
        {
            request.PartitionNames.AddRange(this.PartitionNames);
        }

        if (this.OutputFields?.Any() == true)
        {
            request.OutputFields.AddRange(this.OutputFields);
        }

        request.GuaranteeTimestamp = (ulong)GuaranteeTimestamp;

        foreach (var parameter in Parameters)
        {
            request.SearchParams.Add(
                new Grpc.KeyValuePair() { Key = parameter.Key, Value = parameter.Value });
        }

        //Prepare target vectors
        Grpc.PlaceholderGroup placeholderGroup = new();
        foreach (var milvusVector in MilvusVectors)
        {
            var plType = Grpc.PlaceholderType.None;
            //TODO Convert vector.

            Grpc.PlaceholderValue placeholderValue = new Grpc.PlaceholderValue();

            placeholderGroup.Placeholders.Add(placeholderValue);
        }
        request.PlaceholderGroup = placeholderGroup.ToByteString();

        request.SearchParams.AddRange(
            new[]
            {
                new Grpc.KeyValuePair() { Key = Constant.VECTOR_FIELD, Value = VectorFieldName },
                new Grpc.KeyValuePair() { Key = Constant.TOP_K, Value = TopK.ToString() },
                new Grpc.KeyValuePair() { Key = Constant.METRIC_TYPE, Value = MetricType.ToString().ToUpper() },
                new Grpc.KeyValuePair() { Key = Constant.ROUND_DECIMAL, Value = RoundDecimal.ToString() },
                new Grpc.KeyValuePair() { Key = Constant.IGNORE_GROWING, Value = IgnoreGrowing.ToString()},
            });

        //dsl
        request.DslType = Grpc.DslType.BoolExprV1;
        if (!string.IsNullOrEmpty(this.Expr))
        {
            request.Dsl = this.Expr;
        }

        return request;
    }

    /// <summary>
    /// Validate search parameter.
    /// </summary>
    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.True(GuaranteeTimestamp > 0, "The guarantee timestamp must be greater than 0");
        Verify.True(MetricType != MetricType.Invalid, "Metric type is invalid");
        Verify.True(MilvusVectors.Count > 0, "Target vectors can not be empty");
    }

    #region Private ======================================================
    private SearchParameters(string collectionName) 
    {
        CollectionName = collectionName;
    }
    #endregion
}
