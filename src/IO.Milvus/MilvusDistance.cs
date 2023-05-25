using IO.Milvus.Grpc;
using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// Milvus distance
/// </summary>
public class MilvusCalDistanceResult
{
    public IList<int> IntDistance { get; }

    public IList<float> FloatDistance { get; }

    internal static MilvusCalDistanceResult From(Grpc.CalcDistanceResults calcDistanceResults)
    {
        return new MilvusCalDistanceResult(calcDistanceResults.IntDist, calcDistanceResults.FloatDist);
    }

    #region Private =====================================================================================
    private MilvusCalDistanceResult(IntArray intDist, FloatArray floatDist)
    {
        IntDistance = intDist.Data;
        FloatDistance = floatDist.Data;
    }
    #endregion
}
