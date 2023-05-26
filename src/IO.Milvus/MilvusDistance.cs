using IO.Milvus.ApiSchema;
using IO.Milvus.Grpc;
using System.Collections.Generic;

namespace IO.Milvus;

/// <summary>
/// Milvus distance
/// </summary>
public class MilvusCalDistanceResult
{
    /// <summary>
    /// Int distance value.
    /// </summary>
    public IList<int> IntDistance { get; }

    /// <summary>
    /// Float distance value.
    /// </summary>
    public IList<float> FloatDistance { get; }

    internal static MilvusCalDistanceResult From(Grpc.CalcDistanceResults calcDistanceResults)
    {
        return new MilvusCalDistanceResult(calcDistanceResults.IntDist, calcDistanceResults.FloatDist);
    }

    internal static MilvusCalDistanceResult From(CalDistanceResponse data)
    {
        return new MilvusCalDistanceResult(null, null);
    }

    #region Private =======================================================================
    private MilvusCalDistanceResult(IntArray intDistance, FloatArray floatDistance)
    {
        IntDistance = intDistance.Data;
        FloatDistance = floatDistance.Data;
    }
    #endregion
}
