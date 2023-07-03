using IO.Milvus.Diagnostics;
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
        Verify.NotNullOrEmpty(floatVectors);

        FloatArray floatArray = new();

        int dim = floatVectors[0].Count;

        foreach (List<float> value in floatVectors)
        {
            floatArray.Data.AddRange(value);
        }

        return (floatArray, dim);
    }

    /// <summary>Creates a <see cref="Dictionary{TKey, TValue}"/> from GRPC KeyValuePairs.</summary>
    public static Dictionary<string, string> ToDictionary(this IEnumerable<Grpc.KeyValuePair> source) =>
        source.ToDictionary(static p => p.Key, static p => p.Value);
}
