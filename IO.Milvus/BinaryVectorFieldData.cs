using System.Buffers;
using System.Diagnostics;

namespace IO.Milvus;

/// <summary>
/// Binary Field
/// </summary>
public sealed class BinaryVectorFieldData : FieldData<ReadOnlyMemory<byte>>
{
    /// <summary>
    /// Construct a binary vector field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="bytes"></param>
    public BinaryVectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<byte>> bytes)
        : base(fieldName, bytes, MilvusDataType.BinaryVector, false)
    {
    }

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        int dataCount = Data.Count;
        if (dataCount == 0)
        {
            throw new MilvusException("The number of vectors must be positive.");
        }

        int vectorByteLength = Data[0].Length;
        int totalByteLength = vectorByteLength;
        for (int i = 1; i < dataCount; i++)
        {
            int rowLength = Data[i].Length;
            if (rowLength != vectorByteLength)
            {
                throw new MilvusException("All vectors must have the same dimensionality.");
            }

            checked { totalByteLength += rowLength; }
        }

        byte[] bytes = ArrayPool<byte>.Shared.Rent(totalByteLength);
        int pos = 0;
        for (int i = 0; i < dataCount; i++)
        {
            ReadOnlyMemory<byte> row = Data[i];
            row.Span.CopyTo(bytes.AsSpan(pos, row.Length));
            pos += row.Length;
        }
        Debug.Assert(pos == totalByteLength);

        var result = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                BinaryVector = ByteString.CopyFrom(bytes.AsSpan(0, totalByteLength)),
                Dim = vectorByteLength * 8,
            }
        };

        ArrayPool<byte>.Shared.Return(bytes);

        return result;
    }
}
