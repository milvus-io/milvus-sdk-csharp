namespace IO.Milvus;

/// <summary>
/// ByteString Field
/// </summary>
public sealed class ByteStringFieldData : FieldData
{
    /// <summary>
    /// Construct a ByteString field
    /// </summary>
    public ByteStringFieldData(
        string fieldName,
        ByteString byteString,
        long dimension) :
        base(fieldName, MilvusDataType.BinaryVector)
    {
        DataType = MilvusDataType.BinaryVector;
        ByteString = byteString;
        RowCount = dimension;
    }

    /// <summary>
    /// ByteString
    /// </summary>
    public ByteString ByteString { get; set; }

    /// <inheritdoc />
    public override long RowCount { get; protected set; }

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        return new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                BinaryVector = ByteString,
                Dim = RowCount,
            }
        };
    }
}
