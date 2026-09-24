using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class MilvusSparseVectorTests
{
    [Fact]
    public void Constructor_stores_indices_and_values()
    {
        var vector = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 2, 5 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, 2.5f, -3.0f }));

        Assert.Equal(3, vector.Count);
        Assert.Equal(new[] { 0, 2, 5 }, vector.Indices.ToArray());
        Assert.Equal(new[] { 1.0f, 2.5f, -3.0f }, vector.Values.ToArray());
    }

    [Fact]
    public void Constructor_rejects_mismatched_lengths()
    {
        Assert.Throws<ArgumentException>(() => new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 1 }),
            new ReadOnlyMemory<float>(new[] { 1.0f })));
    }

    [Fact]
    public void Empty_vector_has_zero_count()
    {
        var vector = new MilvusSparseVector<float>(ReadOnlyMemory<int>.Empty, ReadOnlyMemory<float>.Empty);
        Assert.Equal(0, vector.Count);
    }

    [Fact]
    public void ToString_reports_count()
    {
        var vector = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 1 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, 2.0f }));

        Assert.Equal("MilvusSparseVector<Single>(Count=2)", vector.ToString());
    }

    [Fact]
    public void Equals_compares_indices_and_values()
    {
        var a = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 2 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, -2.5f }));
        var b = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 2 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, -2.5f }));
        var differentValues = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 2 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, -2.0f }));
        var differentIndices = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 3 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, -2.5f }));

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.NotEqual(a, differentValues);
        Assert.False(a == differentValues);
        Assert.True(a != differentValues);

        Assert.NotEqual(a, differentIndices);
        Assert.False(a == differentIndices);
    }

    [Fact]
    public void ToBytes_encodes_little_endian_index_and_float_pairs()
    {
        var vector = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 1 }),
            new ReadOnlyMemory<float>(new[] { 1.0f, -2.0f }));

        byte[] bytes = vector.ToBytes();

        Assert.Equal(2 * 8, bytes.Length);
        // index 0 (uint32 LE) + 1.0f (0x3F800000, LE)
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F }, bytes.AsSpan(0, 8).ToArray());
        // index 1 (uint32 LE) + -2.0f (0xC0000000, LE)
        Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0 }, bytes.AsSpan(8, 8).ToArray());
    }

    [Fact]
    public void ToBytes_FromBytes_roundtrips()
    {
        var vector = new MilvusSparseVector<float>(
            new ReadOnlyMemory<int>(new[] { 0, 2, 5, 100 }),
            new ReadOnlyMemory<float>(new[] { 1.5f, -2.25f, 3.75f, 0.125f }));

        byte[] bytes = vector.ToBytes();
        Assert.Equal(vector.Count * 8, bytes.Length);

        MilvusSparseVector<float> back = MilvusSparseVector<float>.FromBytes(bytes);

        Assert.Equal(vector, back);
        Assert.Equal(vector.Indices.ToArray(), back.Indices.ToArray());
        Assert.Equal(vector.Values.ToArray(), back.Values.ToArray());
    }

    [Fact]
    public void Empty_vector_roundtrips()
    {
        var empty = new MilvusSparseVector<float>(ReadOnlyMemory<int>.Empty, ReadOnlyMemory<float>.Empty);

        byte[] bytes = empty.ToBytes();
        Assert.Empty(bytes);

        MilvusSparseVector<float> back = MilvusSparseVector<float>.FromBytes(bytes);
        Assert.Equal(0, back.Count);
    }

    [Fact]
    public void ToBytes_rejects_non_float_element_type()
    {
        var vector = new MilvusSparseVector<double>(
            new ReadOnlyMemory<int>(new[] { 0 }),
            new ReadOnlyMemory<double>(new[] { 1.0 }));

        Assert.Throws<NotSupportedException>(() => vector.ToBytes());
    }

    [Fact]
    public void FromBytes_rejects_non_float_element_type()
    {
        Assert.Throws<NotSupportedException>(() => MilvusSparseVector<double>.FromBytes(new byte[8]));
    }

    [Fact]
    public void FromBytes_rejects_length_not_multiple_of_eight()
    {
        Assert.Throws<ArgumentException>(() => MilvusSparseVector<float>.FromBytes(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void FromBytes_rejects_index_exceeding_int_max()
    {
        // index = 2^31 (does not fit in int) followed by a float value
        byte[] bytes = new byte[8];
        bytes[0] = 0x00; bytes[1] = 0x00; bytes[2] = 0x00; bytes[3] = 0x80; // 0x80000000
        bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x80; bytes[7] = 0x3F; // 1.0f

        Assert.Throws<NotSupportedException>(() => MilvusSparseVector<float>.FromBytes(bytes));
    }

    [Fact]
    public void Equals_and_hash_code_match_value()
    {
        var a = new MilvusSparseVector<float>(new[] { 1, 2 }, new[] { 0.5f, 1.5f });
        var b = new MilvusSparseVector<float>(new[] { 1, 2 }, new[] { 0.5f, 1.5f });
        var different = new MilvusSparseVector<float>(new[] { 1, 2 }, new[] { 0.5f, 2.5f });

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(different));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
