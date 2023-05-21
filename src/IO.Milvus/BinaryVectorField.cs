using IO.Milvus.Exception;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Binary Field
/// </summary>
public class BinaryVectorField : Field
{
    /// <summary>
    /// Vector data
    /// </summary>
    public IList<IList<float>> Data { get; set; }

    ///<inheritdoc/>
    public override int RowCount => Data?.Count ?? 0;

    ///<inheritdoc/>
    public override Grpc.FieldData ToGrpcFieldData()
    {
        var floatArray = new Grpc.FloatArray();

        var count = Data.First().Count;
        if (!Data.All(p => p.Count == count))
        {
            throw new ParamException("Row count of fields must be equal");
        }
        foreach (var data in Data)
        {
            floatArray.Data.AddRange(data);
        }

        return new Grpc.FieldData()
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField()
            {
                FloatVector = floatArray,
                Dim = count,
            },
        };
    }
}
