using Google.Protobuf;
using IO.Milvus.ApiSchema;

namespace IO.Milvus;

/// <summary>
/// ByteString Field
/// </summary>
public class ByteStringField : Field
{
    /// <summary>
    /// Construct a ByteString field
    /// </summary>
    public ByteStringField()
    {
        DataType = MilvusDataType.BinaryVector;
    }

    /// <summary>
    /// ByteString
    /// </summary>
    public ByteString ByteString { get; set; }

    ///<inheritdoc/>
    public override int RowCount => 0;

    ///<inheritdoc/>
    public override Grpc.FieldData ToGrpcFieldData()
    {
        return new Grpc.FieldData()
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField()
            {
                BinaryVector = ByteString,
            }
        };
    }
}