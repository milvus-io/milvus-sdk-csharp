using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class CalcDistanceRequest
{
    private readonly MilvusMetricType _milvusMetricType;

    /// <summary>
    /// Left vectors to calculate distance.
    /// </summary>
    [JsonPropertyName("op_left")]
    public MilvusVectors VectorsLeft { get; set; }

    /// <summary>
    /// Right vectors to calculate distance.
    /// </summary>
    [JsonPropertyName("op_right")]
    public MilvusVectors VectorsRight { get; set; }

    [JsonPropertyName("params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> Params { get; set; } = new Dictionary<string, string>();

    internal static CalcDistanceRequest Create(MilvusMetricType milvusMetricType)
    {
        return new CalcDistanceRequest(milvusMetricType);
    }

    public CalcDistanceRequest WithLeftVectors(MilvusVectors vectors)
    {
        VectorsLeft = vectors;
        return this;
    }

    public CalcDistanceRequest WithRightVectors(MilvusVectors vectors)
    {
        VectorsRight = vectors;
        return this;
    }

    public Grpc.CalcDistanceRequest BuildGrpc()
    {
        Validate();
        //Create a request
        var request = new Grpc.CalcDistanceRequest();

        //Set left vectors
        request.OpLeft = VectorsLeft.ToVectorsArray();

        //Set right vectors
        request.OpRight = VectorsRight.ToVectorsArray();

        //Set parameters
        request.Params.Add(new Grpc.KeyValuePair()
        {
            Key = "metric",
            Value = _milvusMetricType.ToString().ToUpperInvariant()
        });

        return request;
    }

    public void Validate()
    {
        Verify.NotNull(VectorsLeft);
        Verify.NotNull(VectorsRight);
        if (_milvusMetricType is not MilvusMetricType.L2 and not MilvusMetricType.IP and not MilvusMetricType.Hamming and not MilvusMetricType.Tanimoto)
        {
            throw new ArgumentOutOfRangeException(nameof(_milvusMetricType), "MetricType must be one of \"metric\":\"L2\"/\"IP\"/\"HAMMIN\"/\"TANIMOTO\"");
        }
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/distance",
            payload: this);
    }

    #region Private =======================================================================
    private CalcDistanceRequest(MilvusMetricType milvusMetricType)
    {
        _milvusMetricType = milvusMetricType;
        Params["metric"] = milvusMetricType.ToString().ToUpperInvariant();
    }
    #endregion
}