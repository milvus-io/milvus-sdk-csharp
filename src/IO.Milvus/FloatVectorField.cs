using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Float vector field
/// </summary>
public class FloatVectorField:Field<List<float>>
{
    /// <summary>
    /// Create a float vector field.
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">data</param>
    public FloatVectorField(
        string fieldName, 
        IList<List<float>> data) : 
        base(fieldName, data, MilvusDataType.FloatVector)
    {
    }

    /// <summary>
    /// Row count.
    /// </summary>
    public override long RowCount => Data?.Count ?? 0;

    /// <summary>
    /// Convert to grpc field data
    /// </summary>
    /// <returns>Field data</returns>
    /// <exception cref="Diagnostics.MilvusException"></exception>
    public override Grpc.FieldData ToGrpcFieldData()
    {
        var floatArray = new Grpc.FloatArray();

        var dim = Data.First().Count;
        if (!Data.All(p => p.Count == dim))
        {
            throw new Diagnostics.MilvusException("Row count of fields must be equal");
        }
        foreach (var value in Data)
        {
            floatArray.Data.AddRange(value);
        }

        return new Grpc.FieldData()
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField()
            {
                FloatVector = floatArray,
                Dim = dim,
            },
        };
    }
}
