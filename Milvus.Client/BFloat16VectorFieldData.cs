using System.Buffers;
using System.Diagnostics;

namespace Milvus.Client;

/// <summary>
/// BFloat16 Vector Field. Available since Milvus v2.4.
/// </summary>
public sealed class BFloat16VectorFieldData : FieldData<ReadOnlyMemory<BFloat16>>
{
    /// <summary>
    /// Construct a bfloat16 vector field
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">Vector data</param>
    public BFloat16VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<BFloat16>> data)
        : base(fieldName, data, MilvusDataType.BFloat16Vector, false)
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
            if (Data[i].Length != vectorDimension)
            {
                throw new MilvusException("All vectors must have the same dimensionality.");
            }

            checked { totalByteLength += vectorByteLength; }
        }

        byte[] bytes = ArrayPool<byte>.Shared.Rent(totalByteLength);
        int pos = 0;

        for (int i = 0; i < dataCount; i++)
        {
            ReadOnlySpan<BFloat16> rowSpan = Data[i].Span;

            for (int j = 0; j < rowSpan.Length; j++)
            {
                ushort bits = rowSpan[j].ToBits();
                bytes[pos] = (byte)bits;
                bytes[pos + 1] = (byte)(bits >> 8);
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
                Bfloat16Vector = ByteString.CopyFrom(bytes.AsSpan(0, totalByteLength)),
                Dim = vectorDimension,
            }
        };

        ArrayPool<byte>.Shared.Return(bytes);

        return result;
    }

    internal override object GetValueAsObject(int index)
        => throw new NotSupportedException("Dynamic vector fields are not supported");
}
