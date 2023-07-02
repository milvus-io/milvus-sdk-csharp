using IO.Milvus.ApiSchema;
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
        return new MilvusCalDistanceResult(
            calcDistanceResults.IntDist?.Data,
            calcDistanceResults.FloatDist?.Data);
    }

    internal static MilvusCalDistanceResult From(CalDistanceResponse data)
    {
        return new MilvusCalDistanceResult(
            data.MilvusDistance?.IntDistance?.Data,
            data.MilvusDistance?.FloatDistance?.Data);
    }

    #region Private =======================================================================
    private MilvusCalDistanceResult(IList<int> intDistance, IList<float> floatDistance)
    {
        IntDistance = intDistance;
        FloatDistance = floatDistance;
    }
    #endregion
}
