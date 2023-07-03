using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;

namespace IO.Milvus.Utils;

/// <summary>
/// Field utils.
/// </summary>
public static class FieldUtils
{
    /// <summary>
    /// Convert to float array.
    /// </summary>
    /// <param name="floatVectors">Float vectors</param>
    /// <returns>Floatarray and dimension</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static (FloatArray, int) ToFloatArray(
        this IList<List<float>> floatVectors)
    {
        Verify.NotNullOrEmpty(floatVectors);

        var floatArray = new FloatArray();

        int dim = floatVectors[0].Count;

        foreach (List<float> value in floatVectors)
        {
            floatArray.Data.AddRange(value);
        }

        return (floatArray, dim);
    }
}
