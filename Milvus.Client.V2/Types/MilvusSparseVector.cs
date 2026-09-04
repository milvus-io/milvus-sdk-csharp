using System.Buffers.Binary;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents a sparse vector using COO (Coordinate) format: a set of (index, value) pairs with non-negative,
/// ascending indices.
/// </summary>
/// <typeparam name="T">The type of the values in the vector.</typeparam>
public readonly struct MilvusSparseVector<T> : IEquatable<MilvusSparseVector<T>>
{
    private readonly ReadOnlyMemory<int> _indices;
    private readonly ReadOnlyMemory<T> _values;

    /// <summary>
    /// Creates a sparse vector from parallel collections of indices and values. The indices must be non-negative
    /// and sorted in ascending order.
    /// </summary>
    /// <param name="indices">The indices of non-zero elements, sorted in ascending order.</param>
    /// <param name="values">The values of non-zero elements.</param>
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
    /// The number of non-zero elements.
    /// </summary>
    public int Count => _indices.Length;

    /// <summary>
    /// The indices of non-zero elements, in ascending order.
    /// </summary>
    public ReadOnlyMemory<int> Indices => _indices;

    /// <summary>
    /// The values of non-zero elements, in the same order as <see cref="Indices" />.
    /// </summary>
    public ReadOnlyMemory<T> Values => _values;

    /// <inheritdoc />
    public override string ToString() => $"MilvusSparseVector<{typeof(T).Name}>(Count={Count})";

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MilvusSparseVector<T> other && Equals(other);

    /// <inheritdoc />
    public bool Equals(MilvusSparseVector<T> other)
    {
        if (!_indices.Span.SequenceEqual(other._indices.Span))
        {
            return false;
        }

        ReadOnlySpan<T> values = _values.Span;
        ReadOnlySpan<T> otherValues = other._values.Span;
        if (values.Length != otherValues.Length)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (!values[i]!.Equals(otherValues[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var h = new HashCode();
        foreach (int index in _indices.Span)
        {
            h.Add(index);
        }
        foreach (T value in _values.Span)
        {
            h.Add(value);
        }
        return h.ToHashCode();
    }

    /// <summary>Indicates whether two sparse vectors are equal.</summary>
    public static bool operator ==(MilvusSparseVector<T> left, MilvusSparseVector<T> right)
        => left.Equals(right);

    /// <summary>Indicates whether two sparse vectors are not equal.</summary>
    public static bool operator !=(MilvusSparseVector<T> left, MilvusSparseVector<T> right)
        => !left.Equals(right);

    /// <summary>
    /// Serializes the sparse vector to the Milvus wire format: a sequence of little-endian (index: uint32,
    /// value: float32) pairs, sorted by index.
    /// </summary>
    internal byte[] ToBytes()
    {
        if (typeof(T) != typeof(float))
        {
            throw new NotSupportedException($"Serialization not supported for type {typeof(T)}");
        }

        ReadOnlySpan<int> indices = _indices.Span;
        ReadOnlySpan<float> values = ((ReadOnlyMemory<float>)(object)_values).Span;
        byte[] result = new byte[indices.Length * 8];

        for (int i = 0; i < indices.Length; i++)
        {
            int offset = i * 8;
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), (uint)indices[i]);
            WriteSingleLittleEndian(result.AsSpan(offset + 4), values[i]);
        }

        return result;
    }

    /// <summary>
    /// Deserializes a sparse vector from the Milvus wire format.
    /// </summary>
    internal static MilvusSparseVector<T> FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (typeof(T) != typeof(float))
        {
            throw new NotSupportedException($"Deserialization not supported for type {typeof(T)}");
        }

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
            values[i] = ReadSingleLittleEndian(bytes.Slice(offset + 4, 4));
        }

        return (MilvusSparseVector<T>)(object)new MilvusSparseVector<float>(indices, values);
    }

    private static void WriteSingleLittleEndian(Span<byte> destination, float value)
    {
#if NET8_0_OR_GREATER
        BinaryPrimitives.WriteSingleLittleEndian(destination, value);
#else
        byte[] tmp = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(tmp);
        }
        tmp.CopyTo(destination);
#endif
    }

    private static float ReadSingleLittleEndian(ReadOnlySpan<byte> source)
    {
#if NET8_0_OR_GREATER
        return BinaryPrimitives.ReadSingleLittleEndian(source);
#else
        byte[] tmp = source.ToArray();
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(tmp);
        }
        return BitConverter.ToSingle(tmp, 0);
#endif
    }
}
