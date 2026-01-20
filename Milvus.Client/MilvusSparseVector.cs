using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Milvus.Client;

/// <summary>
/// Represents a sparse vector using COO (Coordinate) format.
/// Each element is a pair of (index, value) where index is a non-negative integer.
/// </summary>
/// <typeparam name="T">The type of the values in the vector.</typeparam>
public readonly struct MilvusSparseVector<T> : IEquatable<MilvusSparseVector<T>>
{
    private readonly ReadOnlyMemory<int> _indices;
    private readonly ReadOnlyMemory<T> _values;

    /// <summary>
    /// Creates a sparse vector from parallel collections of indices and values. The indices must be positive
    /// and sorted in ascending order.
    /// </summary>
    /// <param name="indices">The indices of non-zero elements, sorted in ascending order.</param>
    /// <param name="values">The values of non-zero elements.</param>
    /// <exception cref="ArgumentException">Thrown when collections have different lengths.</exception>
    public MilvusSparseVector(ReadOnlyMemory<int> indices, ReadOnlyMemory<T> values)
    {
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
    public ReadOnlyMemory<int> Indices => _indices;

    /// <summary>
    /// Gets the values of non-zero elements, in the same order as <see cref="Indices"/>.
    /// </summary>
    public ReadOnlyMemory<T> Values => _values;

    /// <summary>
    /// Gets the maximum index in the sparse vector, or -1 if the vector is empty.
    /// </summary>
    internal int MaxIndex => _indices.Length > 0 ? _indices.Span[_indices.Length - 1] : -1;

    /// <inheritdoc />
    public override string ToString()
        => $"MilvusSparseVector<{typeof(T).Name}>(Count={Count})";

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is MilvusSparseVector<T> other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(MilvusSparseVector<T> other)
    {
#if NET8_0_OR_GREATER
        return _indices.Span.SequenceEqual(other._indices.Span)
            && _values.Span.SequenceEqual(other._values.Span);
#else // Older versions only have a SequenceEqual with an IEquatable constraint.
        if (!_indices.Span.SequenceEqual(other._indices.Span))
        {
            return false;
        }

        var values = _values.Span;
        var otherValues = other._values.Span;
        if (values.Length != otherValues.Length)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i += 1)
        {
            if (!values[i]!.Equals(otherValues[i]))
            {
                return false;
            }
        }

        return true;
#endif
    }

    /// <summary>Indicates whether the two vectors are equal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is equal to <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator ==(MilvusSparseVector<T> left, MilvusSparseVector<T> right)
        => left.Equals(right);

    /// <summary>Indicates whether the two vectors are not equal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is not equal <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator !=(MilvusSparseVector<T> left, MilvusSparseVector<T> right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode h = new();
        ReadOnlySpan<int> indices = _indices.Span;
        ReadOnlySpan<T> values = _values.Span;
        for (int i = 0; i < indices.Length; i += 1)
        {
            h.Add(indices[i]);
            h.Add(values[i]);
        }

        return h.ToHashCode();
    }

    /// <summary>
    /// Serializes the sparse vector to bytes in the format used by Milvus.
    /// The format is a sequence of (index: uint32, value: float32) pairs, sorted by index.
    /// </summary>
    internal byte[] ToBytes()
    {
        if (typeof(T) == typeof(float))
        {
            ReadOnlySpan<int> indices = _indices.Span;
            ReadOnlySpan<float> values = ((ReadOnlyMemory<float>)(object)_values).Span;
            byte[] result = new byte[indices.Length * 8]; // 4 bytes for index + 4 bytes for value

            for (int i = 0; i < indices.Length; i++)
            {
                int offset = i * 8;
                BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), (uint)indices[i]);
#if NET8_0_OR_GREATER
                BinaryPrimitives.WriteSingleLittleEndian(result.AsSpan(offset + 4), values[i]);
#else
                if (BitConverter.IsLittleEndian)
                {
                    float value = values[i];
                    MemoryMarshal.Write(result.AsSpan(offset + 4), ref value);
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

