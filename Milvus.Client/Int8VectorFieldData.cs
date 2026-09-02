using System.Buffers;
using System.Diagnostics;

namespace Milvus.Client;

/// <summary>
/// Int8 Vector Field
/// </summary>
public sealed class Int8VectorFieldData : FieldData<ReadOnlyMemory<sbyte>>
{
    /// <summary>
    /// Construct an int8 vector field
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">Vector data</param>
    public Int8VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<sbyte>> data)
        : base(fieldName, data, MilvusDataType.Int8Vector, false)
    {
    }

    /// <summary>
    /// Row count.
    /// </summary>
    public override long RowCount => Data.Count;

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        int dataCount = Data.Count;
        if (dataCount == 0)
        {
            throw new MilvusException("The number of vectors must be positive.");
        }

        int vectorDimension = Data[0].Length;
        int totalByteLength = vectorDimension;

        for (int i = 1; i < dataCount; i++)
        {
            int rowLength = Data[i].Length;
            if (rowLength != vectorDimension)
            {
                throw new MilvusException("All vectors must have the same dimensionality.");
            }

            checked { totalByteLength += vectorDimension; }
        }

        byte[] bytes = ArrayPool<byte>.Shared.Rent(totalByteLength);
        int pos = 0;

        for (int i = 0; i < dataCount; i++)
        {
            ReadOnlySpan<sbyte> rowSpan = Data[i].Span;

            for (int j = 0; j < rowSpan.Length; j++)
            {
                // Each int8 vector element is a single signed byte on the wire; reinterpreting the
                // two's-complement bit pattern as an unsigned byte round-trips correctly on read.
                bytes[pos++] = unchecked((byte)rowSpan[j]);
            }
        }
        Debug.Assert(pos == totalByteLength);

        var result = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                Int8Vector = ByteString.CopyFrom(bytes.AsSpan(0, totalByteLength)),
                Dim = vectorDimension,
            }
        };

        ArrayPool<byte>.Shared.Return(bytes);

        return result;
    }

    internal override object GetValueAsObject(int index)
        => throw new NotSupportedException("Dynamic vector fields are not supported");
}
