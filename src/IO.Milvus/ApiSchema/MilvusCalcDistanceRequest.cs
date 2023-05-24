using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using System;
using System.Text;
using System.Text.Json.Serialization;

namespace IO.Milvus;

public class MilvusCalcDistanceRequest:IGrpcRequest<Grpc.CalcDistanceRequest>
{
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

    public CalcDistanceRequest BuildGrpc()
    {
        var request = new Grpc.CalcDistanceRequest();

        return request;
    }
}