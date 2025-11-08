#if NET8_0_OR_GREATER
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Milvus.Client;

/// <summary>
/// Float16 Vector Field
/// </summary>
public sealed class Float16VectorFieldData : FieldData<ReadOnlyMemory<Half>>
{
    /// <summary>
    /// Construct a float16 vector field
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">Vector data</param>
    public Float16VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<Half>> data)
        : base(fieldName, data, MilvusDataType.Float16Vector, false)
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
        int vectorByteLength = vectorDimension * sizeof(ushort);
        int totalByteLength = vectorByteLength;

        for (int i = 1; i < dataCount; i++)
        {
            int rowLength = Data[i].Length;
            if (rowLength != vectorDimension)
            {
                throw new MilvusException("All vectors must have the same dimensionality.");
            }

            checked { totalByteLength += vectorByteLength; }
        }

        byte[] bytes = ArrayPool<byte>.Shared.Rent(totalByteLength);
        int pos = 0;

        for (int i = 0; i < dataCount; i++)
        {
            ReadOnlyMemory<Half> row = Data[i];
            ReadOnlySpan<Half> rowSpan = row.Span;

            for (int j = 0; j < rowSpan.Length; j++)
            {
                ushort halfBits = BitConverter.HalfToUInt16Bits(rowSpan[j]);
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(pos), halfBits);
                pos += sizeof(ushort);
            }
        }
        Debug.Assert(pos == totalByteLength);

        var result = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                Float16Vector = ByteString.CopyFrom(bytes.AsSpan(0, totalByteLength)),
                Dim = vectorDimension,
            }
        };

        ArrayPool<byte>.Shared.Return(bytes);

        return result;
    }

    internal override object GetValueAsObject(int index)
        => throw new NotSupportedException("Dynamic vector fields are not supported");
}
#endif
