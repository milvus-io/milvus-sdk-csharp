using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;

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
        if (floatVectors is null)
        {
            throw new ArgumentNullException(nameof(floatVectors));
        }

        var floatArray = new FloatArray();

        int dim = (int)floatVectors.First().Count;

        foreach (var value in floatVectors)
        {
            floatArray.Data.AddRange(value);
        }

        return (floatArray, dim);
    }
}
