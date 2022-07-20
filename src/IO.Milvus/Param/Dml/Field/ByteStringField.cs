using Google.Protobuf;
using IO.Milvus.Grpc;

namespace IO.Milvus.Param.Dml
{
    public class ByteStringField : Field
    {
        public ByteStringField()
        {
            DataType = DataType.BinaryVector;
        }

        public ByteString Bytes { get; set; }

        public override int RowCount => 0;

        public override FieldData ToGrpcFieldData()
        {
            return new FieldData()
            {
                FieldName = FieldName,
                Type = DataType,
                Vectors = new VectorField()
                {
                    BinaryVector = Bytes,                    
                }
            };
        }
    }
}
