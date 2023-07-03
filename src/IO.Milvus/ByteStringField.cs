using Google.Protobuf;

namespace IO.Milvus;

/// <summary>
/// ByteString Field
/// </summary>
public sealed class ByteStringField : Field
{
    /// <summary>
    /// Construct a ByteString field
    /// </summary>
    public ByteStringField(
        string fieldName,
        ByteString byteString,
        long dimension) :
        base(fieldName, MilvusDataType.BinaryVector)
    {
        DataType = MilvusDataType.BinaryVector;
        RowCount = dimension;
    }

    /// <summary>
    /// ByteString
    /// </summary>
    public ByteString ByteString { get; set; }

    ///<inheritdoc/>
    public override long RowCount { get; protected set; }

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
                Dim = RowCount,
            }
        };
    }
}