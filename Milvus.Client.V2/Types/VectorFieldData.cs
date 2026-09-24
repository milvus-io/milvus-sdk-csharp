using System.Buffers.Binary;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A <see cref="DataType.FloatVector" /> field's data: one <see cref="ReadOnlyMemory{T}" /> of <see cref="float" /> per row.
/// </summary>
public sealed class FloatVectorFieldData : FieldData<ReadOnlyMemory<float>>
{
    /// <summary>
    /// Creates a new float vector field data instance.
    /// </summary>
    public FloatVectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<float>> data)
        : base(fieldName, data, DataType.FloatVector)
    {
    }

    internal FloatVectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<float>> data, bool isDynamic)
        : base(fieldName, data, DataType.FloatVector, isDynamic)
    {
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new FloatVectorFieldData(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.FloatVector,
            IsDynamic = IsDynamic,
            Vectors = new Grpc.VectorField()
        };

        var floatArray = new Grpc.FloatArray();
        int dim = VectorFieldDataValidation.ValidateUniformRowLength(Data, "FloatVector", ValidData);
        for (int i = 0; i < Data.Count; i++)
        {
            if (!IsRowValid(i))
            {
                continue;
            }

            floatArray.Data.AddRange(Data[i].ToArray());
        }

        field.Vectors.Dim = dim;
        field.Vectors.FloatVector = floatArray;
        AddValidData(field);

        return field;
    }
}

/// <summary>
/// A <see cref="DataType.BinaryVector" /> field's data: one <see cref="ReadOnlyMemory{T}" /> of <see cref="byte" /> per row.
/// </summary>
public sealed class BinaryVectorFieldData : FieldData<ReadOnlyMemory<byte>>
{
    /// <summary>
    /// Creates a new binary vector field data instance.
    /// </summary>
    public BinaryVectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<byte>> data)
        : base(fieldName, data, DataType.BinaryVector)
    {
    }

    internal BinaryVectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<byte>> data, bool isDynamic)
        : base(fieldName, data, DataType.BinaryVector, isDynamic)
    {
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new BinaryVectorFieldData(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.BinaryVector,
            IsDynamic = IsDynamic,
            Vectors = new Grpc.VectorField()
        };

        field.Vectors.Dim = VectorFieldDataValidation.ValidateUniformRowLength(Data, "BinaryVector", ValidData) * 8;
        var packed = new List<byte>();
        for (int i = 0; i < Data.Count; i++)
        {
            if (!IsRowValid(i))
            {
                continue;
            }

            packed.AddRange(Data[i].ToArray());
        }

        field.Vectors.BinaryVector = ByteString.CopyFrom(packed.ToArray());
        AddValidData(field);
        return field;
    }
}

/// <summary>
/// A <see cref="DataType.SparseFloatVector" /> field's data: one <see cref="MilvusSparseVector{T}" /> of <see cref="float" />
/// per row.
/// </summary>
public sealed class SparseFloatVectorFieldData : FieldData<MilvusSparseVector<float>>
{
    /// <summary>
    /// Creates a new sparse float vector field data instance.
    /// </summary>
    public SparseFloatVectorFieldData(string fieldName, IReadOnlyList<MilvusSparseVector<float>> data)
        : base(fieldName, data, DataType.SparseFloatVector)
    {
    }

    internal SparseFloatVectorFieldData(string fieldName, IReadOnlyList<MilvusSparseVector<float>> data, bool isDynamic)
        : base(fieldName, data, DataType.SparseFloatVector, isDynamic)
    {
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new SparseFloatVectorFieldData(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.SparseFloatVector,
            IsDynamic = IsDynamic,
            Vectors = new Grpc.VectorField()
        };

        long maxDim = 0;
        field.Vectors.SparseFloatVector = new Grpc.SparseFloatArray();
        for (int i = 0; i < Data.Count; i++)
        {
            if (!IsRowValid(i))
            {
                continue;
            }

            MilvusSparseVector<float> row = Data[i];
            field.Vectors.SparseFloatVector.Contents.Add(ByteString.CopyFrom(row.ToBytes()));
            int lastIndex = row.Indices.Length == 0 ? -1 : row.Indices.Span[row.Indices.Length - 1];
            // Compute in long to avoid int overflow for a dimension of int.MaxValue + 1.
            maxDim = Math.Max(maxDim, (long)lastIndex + 1);
        }
        field.Vectors.Dim = maxDim;
        AddValidData(field);

        return field;
    }
}

/// <summary>
/// A <see cref="DataType.Float16Vector" /> field's data: one <see cref="ReadOnlyMemory{T}" /> of
/// <see cref="ushort" /> (FP16 bit patterns) per row.
/// </summary>
public sealed class Float16VectorFieldData : FieldData<ReadOnlyMemory<ushort>>
{
    /// <summary>
    /// Creates a new float16 vector field data instance.
    /// </summary>
    public Float16VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<ushort>> data)
        : base(fieldName, data, DataType.Float16Vector)
    {
    }

    internal Float16VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<ushort>> data, bool isDynamic)
        : base(fieldName, data, DataType.Float16Vector, isDynamic)
    {
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new Float16VectorFieldData(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.Float16Vector,
            IsDynamic = IsDynamic,
            Vectors = new Grpc.VectorField()
        };

        int dim = VectorFieldDataValidation.ValidateUniformRowLength(Data, "Float16Vector", ValidData);
        var bytes = new byte[Data.Count * dim * 2];
        int offset = 0;
        for (int i = 0; i < Data.Count; i++)
        {
            if (!IsRowValid(i))
            {
                continue;
            }

            foreach (ushort half in Data[i].Span)
            {
                bytes[offset++] = (byte)(half & 0xFF);
                bytes[offset++] = (byte)(half >> 8);
            }
        }

        field.Vectors.Dim = dim;
        field.Vectors.Float16Vector = ByteString.CopyFrom(bytes, 0, offset);
        AddValidData(field);
        return field;
    }
}

/// <summary>
/// A <see cref="DataType.BFloat16Vector" /> field's data: one <see cref="ReadOnlyMemory{T}" /> of
/// <see cref="ushort" /> (BFloat16 bit patterns) per row.
/// </summary>
public sealed class BFloat16VectorFieldData : FieldData<ReadOnlyMemory<ushort>>
{
    /// <summary>
    /// Creates a new bfloat16 vector field data instance.
    /// </summary>
    public BFloat16VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<ushort>> data)
        : base(fieldName, data, DataType.BFloat16Vector)
    {
    }

    internal BFloat16VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<ushort>> data, bool isDynamic)
        : base(fieldName, data, DataType.BFloat16Vector, isDynamic)
    {
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new BFloat16VectorFieldData(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.Bfloat16Vector,
            IsDynamic = IsDynamic,
            Vectors = new Grpc.VectorField()
        };

        int dim = VectorFieldDataValidation.ValidateUniformRowLength(Data, "BFloat16Vector", ValidData);
        var bytes = new byte[Data.Count * dim * 2];
        int offset = 0;
        for (int i = 0; i < Data.Count; i++)
        {
            if (!IsRowValid(i))
            {
                continue;
            }

            foreach (ushort half in Data[i].Span)
            {
                bytes[offset++] = (byte)(half & 0xFF);
                bytes[offset++] = (byte)(half >> 8);
            }
        }

        field.Vectors.Dim = dim;
        field.Vectors.Bfloat16Vector = ByteString.CopyFrom(bytes, 0, offset);
        AddValidData(field);
        return field;
    }
}

/// <summary>
/// A <see cref="DataType.Int8Vector" /> field's data: one <see cref="ReadOnlyMemory{T}" /> of
/// <see cref="sbyte" /> per row.
/// </summary>
public sealed class Int8VectorFieldData : FieldData<ReadOnlyMemory<sbyte>>
{
    /// <summary>
    /// Creates a new int8 vector field data instance.
    /// </summary>
    public Int8VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<sbyte>> data)
        : base(fieldName, data, DataType.Int8Vector)
    {
    }

    internal Int8VectorFieldData(string fieldName, IReadOnlyList<ReadOnlyMemory<sbyte>> data, bool isDynamic)
        : base(fieldName, data, DataType.Int8Vector, isDynamic)
    {
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new Int8VectorFieldData(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.Int8Vector,
            IsDynamic = IsDynamic,
            Vectors = new Grpc.VectorField()
        };

        int dim = VectorFieldDataValidation.ValidateUniformRowLength(Data, "Int8Vector", ValidData);
        var bytes = new byte[Data.Count * dim];
        int offset = 0;
        for (int i = 0; i < Data.Count; i++)
        {
            if (!IsRowValid(i))
            {
                continue;
            }

            foreach (sbyte value in Data[i].Span)
            {
                bytes[offset++] = unchecked((byte)value);
            }
        }

        field.Vectors.Dim = dim;
        field.Vectors.Int8Vector = ByteString.CopyFrom(bytes, 0, offset);
        AddValidData(field);
        return field;
    }
}

internal static class VectorFieldDataValidation
{
    // Every row of a vector field must have the same dimension; a mismatched row would otherwise corrupt
    // the serialized payload (or overflow the pre-allocated buffer) while advertising an inconsistent Dim.
    internal static int ValidateUniformRowLength<T>(
        IReadOnlyList<ReadOnlyMemory<T>> data, string fieldDataType, IReadOnlyList<bool>? validData = null)
    {
        if (data.Count == 0)
        {
            return 0;
        }

        // Skip rows marked invalid (valid_data=false): they carry an empty payload for nullable vectors.
        int dim = 0;
        for (int i = 0; i < data.Count; i++)
        {
            if (validData is not null && !validData[i])
            {
                continue;
            }

            int rowDim = data[i].Length;
            if (dim == 0)
            {
                dim = rowDim;
            }
            else if (rowDim != dim)
            {
                throw new ArgumentException(
                    $"Row {i} of the {fieldDataType} field has dimension {rowDim}, but {dim} was expected; " +
                    "all rows must have the same dimension.");
            }
        }

        return dim;
    }
}
