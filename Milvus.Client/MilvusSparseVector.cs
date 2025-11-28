using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Milvus.Client;

/// <summary>
/// Represents a sparse vector using COO (Coordinate) format.
/// Each element is a pair of (index, value) where index is a non-negative integer.
/// </summary>
/// <typeparam name="T">The type of the values in the vector.</typeparam>
public class MilvusSparseVector<T>
{
    private readonly int[] _indices;
    private readonly T[] _values;

    /// <summary>
    /// Creates a sparse vector from parallel collections of indices and values. The indices must be positive
    /// and sorted in ascending order.
    /// </summary>
    /// <param name="indices">The indices of non-zero elements, sorted in ascending order.</param>
    /// <param name="values">The values of non-zero elements.</param>
    /// <exception cref="ArgumentException">Thrown when collections have different lengths.</exception>
    public MilvusSparseVector(int[] indices, T[] values)
    {
        Verify.NotNull(indices);
        Verify.NotNull(values);

        if (indices.Length != values.Length)
        {
            throw new ArgumentException($"Indices and values must have the same length: {indices.Length} vs {values.Length}");
        }

        _indices = indices;
        _values = values;
    }

    /// <summary>
    /// Gets the number of non-zero elements in the sparse vector.
    /// </summary>
    public int Count => _indices.Length;

    /// <summary>
    /// Gets the indices of non-zero elements in ascending order.
    /// </summary>
    public IReadOnlyList<int> Indices => _indices;

    /// <summary>
    /// Gets the values of non-zero elements, in the same order as <see cref="Indices"/>.
    /// </summary>
    public IReadOnlyList<T> Values => _values;

    /// <summary>
    /// Gets the maximum index in the sparse vector, or -1 if the vector is empty.
    /// </summary>
    internal int MaxIndex => _indices.Length > 0 ? _indices[_indices.Length - 1] : -1;

    /// <inheritdoc />
    public override string ToString()
        => $"MilvusSparseVector<{typeof(T).Name}>(Count={Count})";

    /// <summary>
    /// Serializes the sparse vector to bytes in the format used by Milvus.
    /// The format is a sequence of (index: uint32, value: float32) pairs, sorted by index.
    /// </summary>
    internal byte[] ToBytes()
    {
        if (typeof(T) == typeof(float))
        {
            float[] values = (float[])(object)_values;
            byte[] result = new byte[_indices.Length * 8]; // 4 bytes for index + 4 bytes for value

            for (int i = 0; i < _indices.Length; i++)
            {
                int offset = i * 8;
                BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), (uint)_indices[i]);
#if NET8_0_OR_GREATER
                BinaryPrimitives.WriteSingleLittleEndian(result.AsSpan(offset + 4), values[i]);
#else
                if (BitConverter.IsLittleEndian)
                {
                    MemoryMarshal.Write(result.AsSpan(offset + 4), ref values[i]);
                }
                else
                {
                    byte[] tmp = BitConverter.GetBytes(values[i]);
                    Array.Reverse(tmp);
                    tmp.CopyTo(result.AsSpan(offset + 4));
                }
#endif
            }

            return result;
        }

        throw new NotSupportedException($"Serialization not supported for type {typeof(T)}");
    }

    /// <summary>
    /// Deserializes a sparse vector from bytes in the Milvus format.
    /// </summary>
    internal static MilvusSparseVector<T> FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (typeof(T) == typeof(float))
        {
            if (bytes.Length % 8 != 0)
            {
                throw new ArgumentException($"Invalid sparse vector byte length: {bytes.Length}, expected multiple of 8");
            }

            int count = bytes.Length / 8;
            int[] indices = new int[count];
            float[] values = new float[count];

            for (int i = 0; i < count; i++)
            {
                int offset = i * 8;
                indices[i] = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4));
#if NET8_0_OR_GREATER
                values[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset + 4, 4));
#else
                if (BitConverter.IsLittleEndian)
                {
                    values[i] = MemoryMarshal.Read<float>(bytes.Slice(offset + 4, 4));
                }
                else
                {
                    byte[] tmp = bytes.Slice(offset + 4, 4).ToArray();
                    Array.Reverse(tmp);
                    values[i] = BitConverter.ToSingle(tmp, 0);
                }
#endif
            }

            return (MilvusSparseVector<T>)(object)new MilvusSparseVector<float>(indices, values);
        }

        throw new NotSupportedException($"Deserialization not supported for type {typeof(T)}");
    }
}

