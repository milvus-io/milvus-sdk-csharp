using Google.Protobuf;
using IO.Milvus.Diagnostics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace IO.Milvus;

/// <summary>
/// Binary Field
/// </summary>
public sealed class BinaryVectorField : Field<byte[]>
{
    /// <summary>
    /// Construct a binary vector field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="bytes"></param>
    public BinaryVectorField(string fieldName, IList<byte[]> bytes)
        : base(fieldName, bytes, MilvusDataType.BinaryVector, false)
    {
    }

    /// <inheritdoc />
    public override Grpc.FieldData ToGrpcFieldData()
    {
        int dataCount = Data.Count;
        if (dataCount <= 0)
        {
            throw new MilvusException("Number of rows must be positive.");
        }

        int dim = Data[0].Length;
        int lengthSum = 0;
        for (int i = 1; i < dataCount; i++)
        {
            int rowLength = Data[i].Length;
            if (rowLength != dim)
            {
                throw new MilvusException("Row count of fields must be equal.");
            }

            checked { lengthSum += rowLength; }
        }

        byte[] bytes = ArrayPool<byte>.Shared.Rent(lengthSum);
        int pos = 0;
        for (int i = 0; i < dataCount; i++)
        {
            byte[] row = Data[i];
            Array.Copy(row, 0, bytes, pos, row.Length);
            pos += row.Length;
        }
        Debug.Assert(pos == lengthSum);

        var result = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                BinaryVector = ByteString.CopyFrom(bytes.AsSpan(0, lengthSum)),
                Dim = dim,
            },
        };

        ArrayPool<byte>.Shared.Return(bytes);

        return result;
    }
}
