using Google.Protobuf;
using System.IO;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Binary Field
/// </summary>
public class BinaryVectorField : Field<byte[]>
{
    internal BinaryVectorField()
    {
        DataType = ApiSchema.MilvusDataType.BinaryVector;
    }

    ///<inheritdoc/>
    public override int RowCount => Data?.Count ?? 0;

    ///<inheritdoc/>
    public override Grpc.FieldData ToGrpcFieldData()
    {
        var floatArray = new Grpc.FloatArray();

        var dim = Data.First().Length;
        if (!Data.All(p => p.Length == dim))
        {
            throw new Diagnostics.MilvusException("Row count of fields must be equal");
        }
        
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);       
        foreach (var value in Data)
        {
            writer.Write(value);
        }

        var byteString = ByteString.CopyFrom(stream.ToArray());

        return new Grpc.FieldData()
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,            
            Vectors = new Grpc.VectorField()
            {
                BinaryVector = byteString,
                Dim = dim,
            },
        };
    }
}
