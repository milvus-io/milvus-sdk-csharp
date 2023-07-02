using Google.Protobuf;
using IO.Milvus.ApiSchema;
using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace IO.Milvus;

/// <summary>
/// Search parameters.
/// </summary>
public class MilvusSearchParameters
{
    /// <summary>
    /// the consistency level used in the query. 
    /// If the consistency level is not specified, 
    /// the default level is ConsistencyLevelEnum.BOUNDED.
    /// </summary>
    public MilvusConsistencyLevel ConsistencyLevel { get; private set; } = MilvusConsistencyLevel.Bounded;

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
    public long TravelTimestamp { get; private set; }

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
    public long GuaranteeTimestamp { get; private set; } = Constants.GUARANTEE_EVENTUALLY_TS;

    /// <summary>
    /// Metric type of ANN searching.
    /// </summary>
    public MilvusMetricType MetricType { get; private set; }

    /// <summary>
    /// The decimal place of the returned results.
    /// </summary>
    public long RoundDecimal { get; private set; } = -1;

    /// <summary>
    /// Parameters
    /// </summary>
    public IDictionary<string,string> Parameters { get; private set; } = new Dictionary<string,string>();

    /// <summary>
    /// Milvus Vector
    /// </summary>
    public IList<List<float>> MilvusFloatVectors { get; private set; }

    /// <summary>
    /// Milvus vector.
    /// </summary>
    public IList<byte[]> MilvusBinaryVectors { get; private set; }

    /// <summary>
    /// Ignore the growing segments to get best search performance. Default is False.
    /// </summary>
    public bool IgnoreGrowing { get; private set; } = false;

    /// <summary>
    /// Database name.
    /// </summary>
    public string DbName { get; private set; }

    /// <summary>
    ///  Create a search parameters.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="vectorFieldName">Vector field name.</param>
    /// <param name="outFields">Out fields. Vector field is not supported.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <returns></returns>
    public static MilvusSearchParameters Create(
        string collectionName,
        string vectorFieldName,
        IList<string> outFields,
        string dbName = Constants.DEFAULT_DATABASE_NAME)
    {
        return new MilvusSearchParameters(collectionName,vectorFieldName,outFields,dbName);
    }

    /// <summary>
    /// Sets the target vectors.
    /// </summary>
    /// <param name="vectors"></param>
    /// <returns></returns>
    public MilvusSearchParameters WithVectors<TVector>(IList<TVector> vectors)
        where TVector : class
    {
        if (typeof(TVector) == typeof(List<float>))
        {
            MilvusFloatVectors = (IList<List<float>>)vectors;
            MilvusBinaryVectors = null;
        }else if (typeof(TVector) == typeof(byte[]))
        {
            MilvusBinaryVectors = (IList<byte[]>)vectors;
            MilvusFloatVectors = null;
        }

            return this;
    }

    /// <summary>
    /// Specifies the output scalar fields (Optional). 
    /// If the output fields are specified, 
    /// the QueryResults returned by query() will contains the values of these fields.
    /// </summary>
    /// <param name="outputFields">The name list of output fields.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MilvusSearchParameters WithOutputFields(IList<string> outputFields)
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
    public MilvusSearchParameters WithPartitionNames(IList<string> partitionNames)
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
    public MilvusSearchParameters WithExpr(string expr)
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
    public MilvusSearchParameters WithConsistencyLevel(MilvusConsistencyLevel consistencyLevel)
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
    public MilvusSearchParameters WithTravelTimestamp(long travelTimestamp)
    {
        this.TravelTimestamp = travelTimestamp;
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
    public MilvusSearchParameters AddOutField(string fieldName)
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
    public MilvusSearchParameters AddPartitionName(string partitionName)
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
    public MilvusSearchParameters WithMetricType(MilvusMetricType metricType)
    {
        this.MetricType = metricType; 
        return this;
    }

    /// <summary>
    /// Specifies the decimal place of the returned results.
    /// </summary>
    /// <param name="decimal">How many digits after the decimal point.</param>
    /// <returns></returns>
    public MilvusSearchParameters WithRoundDecimal(long @decimal)
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
    /// <item>Note: The timestamp is not an absolute timestamp, it is a hybrid value combined by UTC time and internal flags.
    /// We call it TSO, for more information please refer to: <see href="https://github.com/milvus-io/milvus/blob/master/docs/design_docs/20211214-milvus_hybrid_ts.md"/>
    /// Use an operation's TSO to set this parameter, the server will execute search after this operation is finished.
    /// </item>
    /// <item>
    /// Default value is <see cref="Constants.GUARANTEE_EVENTUALLY_TS"/> , server executes search immediately.
    /// </item> 
    /// </list>
    /// </summary>
    /// <remarks>
    /// (Optional).
    /// </remarks>
    /// <param name="guaranteeTimestamp"></param>
    /// <returns></returns>
    public MilvusSearchParameters WithGuaranteeTimestamp(long guaranteeTimestamp)
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
    public MilvusSearchParameters WithVectorFieldName(string vectorFieldName)
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
    public MilvusSearchParameters WithTopK(int topK)
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
    public MilvusSearchParameters WithParameter(string key,string value)
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
    public MilvusSearchParameters WithIgnoreGrowing(bool ignoreGrowing)
    {
        IgnoreGrowing = ignoreGrowing;
        return this;
    }

    /// <summary>
    /// Build a grpc request.
    /// </summary>
    /// <returns></returns>
    public Grpc.SearchRequest BuildGrpc()
    {
        this.Validate();

        Grpc.SearchRequest request = InitSearchRequest();

        //Prepare target vectors
        PrepareTargetVectors(request);

        //Prepare parameters
        PrepareParameters(request);

        //dsl
        SetDsl(request);
        
        return request;
    }

    /// <summary>
    /// Build a rest request.
    /// </summary>
    /// <returns></returns>
    public HttpRequestMessage BuildRest()
    {
        var request = new SearchRequest() 
        { 
            CollectionName = this.CollectionName,
            Dsl = this.Expr,
            DslType = (int)MilvusDslType.BoolExprV1,
            PartitionNames = this.PartitionNames,
            OutputFields = this.OutputFields,
        };

        request.GuaranteeTimestamp = GetGuaranteeTimestamp(ConsistencyLevel, GuaranteeTimestamp, 0);

        PrepareRestTargetVectors(request);

        PrepareRestParameters(request);

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/search",
            payload: request);
    }

    /// <summary>
    /// Validate search parameter.
    /// </summary>
    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.ArgNotNullOrEmpty(VectorFieldName, "Vector field name cannot be null or empty");
        Verify.True(OutputFields?.Any() == true, "Output fields cannot be null or empty");
        Verify.True(GuaranteeTimestamp >= 0, "The guarantee timestamp must be greater than 0");
        Verify.True(MetricType != MilvusMetricType.Invalid, "Metric type is invalid");
        Verify.True(MilvusFloatVectors.Count > 0, "Target vectors can not be empty");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private =======================================================================
    private MilvusSearchParameters(string collectionName, string vectorFieldName, IList<string> outFields, string dbName) 
    {
        this.CollectionName = collectionName;
        this.VectorFieldName = vectorFieldName;
        this.OutputFields = outFields;
        this.DbName = dbName;
    }

    private static long GetGuaranteeTimestamp(
        MilvusConsistencyLevel? consistencyLevel,
        long guaranteeTimestamp,
        long gracefulTime)
    {
        if (consistencyLevel == null)
        {
            return guaranteeTimestamp;
        }

        switch (consistencyLevel)
        {
            case MilvusConsistencyLevel.Strong:
                guaranteeTimestamp = 0L;
                break;
            case MilvusConsistencyLevel.Bounded:
                guaranteeTimestamp = DateTime.UtcNow.ToUtcTimestamp() - gracefulTime;
                break;
            case MilvusConsistencyLevel.Eventually:
                guaranteeTimestamp = 1L;
                break;
        }

        return guaranteeTimestamp;
    }

    private Grpc.SearchRequest InitSearchRequest()
    {
        var request = new Grpc.SearchRequest()
        {
            CollectionName = this.CollectionName,
            TravelTimestamp = (ulong)TravelTimestamp,
        };

        if (this.PartitionNames?.Any() == true)
        {
            request.PartitionNames.AddRange(this.PartitionNames);
        }

        if (this.OutputFields?.Any() == true)
        {
            request.OutputFields.AddRange(this.OutputFields);
        }

        request.GuaranteeTimestamp = (ulong)GetGuaranteeTimestamp(ConsistencyLevel,GuaranteeTimestamp,0);

        return request;
    }

    private void SetDsl(Grpc.SearchRequest request)
    {
        request.DslType = Grpc.DslType.BoolExprV1;
        if (!string.IsNullOrEmpty(this.Expr))
        {
            request.Dsl = this.Expr;
        }
    }

    private void PrepareRestParameters(SearchRequest request)
    {
        request.SearchParams[Constants.VECTOR_FIELD] = VectorFieldName;
        request.SearchParams[Constants.TOP_K] = TopK.ToString();
        request.SearchParams[Constants.METRIC_TYPE] = MetricType.ToString();
        request.SearchParams[Constants.ROUND_DECIMAL] = RoundDecimal.ToString();        
        request.SearchParams[Constants.IGNORE_GROWING] = IgnoreGrowing.ToString();

        if (Parameters?.Any() == true)
        {
            request.SearchParams[Constants.PARAMS] = Parameters.Combine();
        }
    }

    private void PrepareParameters(Grpc.SearchRequest request)
    {
        request.SearchParams.AddRange (
            new[]
            {
                new Grpc.KeyValuePair() { Key = Constants.VECTOR_FIELD, Value = VectorFieldName },
                new Grpc.KeyValuePair() { Key = Constants.TOP_K, Value = TopK.ToString() },
                new Grpc.KeyValuePair() { Key = Constants.METRIC_TYPE, Value = MetricType.ToString().ToUpper() },
                new Grpc.KeyValuePair() { Key = Constants.IGNORE_GROWING, Value = IgnoreGrowing.ToString() },
                new Grpc.KeyValuePair() { Key = Constants.ROUND_DECIMAL, Value = RoundDecimal.ToString() }
            });

        if (Parameters?.Any() == true)
        {
            request.SearchParams.Add(new Grpc.KeyValuePair() { Key = Constants.PARAMS, Value = Parameters.Combine() });
        }
    }

    private void PrepareTargetVectors(Grpc.SearchRequest request)
    {
        Grpc.PlaceholderGroup placeholderGroup = new();

        var placeholderValue = new Grpc.PlaceholderValue()
        {
            Tag = Constants.VECTOR_TAG
        };

        if (MilvusFloatVectors != null)
        {
            placeholderValue.Type = Grpc.PlaceholderType.FloatVector;
            foreach (var milvusVector in MilvusFloatVectors)
            {

                using var memoryStream = new MemoryStream(milvusVector.Count * sizeof(float));
                using var binaryWriter = new BinaryWriter(memoryStream);
                
                for (int i = 0; i < milvusVector.Count; i++)
                    binaryWriter.Write(milvusVector[i]);
                memoryStream.Seek(0, SeekOrigin.Begin);
                placeholderValue.Values.Add(ByteString.FromStream(memoryStream));
            }
        }else if(MilvusBinaryVectors  != null)
        {
            placeholderValue.Type = Grpc.PlaceholderType.BinaryVector;

            foreach (var milvusVector in MilvusBinaryVectors)
            {
                placeholderValue.Values.Add(ByteString.CopyFrom(milvusVector));
            }
        }

        placeholderGroup.Placeholders.Add(placeholderValue);
        request.PlaceholderGroup = placeholderGroup.ToByteString();
    }

    private void PrepareRestTargetVectors(SearchRequest request)
    {
        if (MilvusFloatVectors != null)
        {
            foreach (var milvusVector in MilvusFloatVectors)
            {
                request.SearchVectors.Add(milvusVector);
            }
        }else if(MilvusBinaryVectors != null)
        {
            throw new NotSupportedException();
        }
    }
    #endregion
}