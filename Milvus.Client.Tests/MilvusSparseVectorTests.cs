using Xunit;

namespace Milvus.Client.Tests;

public class MilvusSparseVectorTests
{
    [Fact]
    public void SparseFloatVector_from_enumerable()
    {
        var vector = new MilvusSparseVector<float>(new Dictionary<int, float>
        {
            [0] = 1.0f,
            [5] = 2.5f,
            [10] = 0.5f
        });

        Assert.Equal(3, vector.Count);
        Assert.Equal(new[] { 0, 5, 10 }, vector.Indices);
        Assert.Equal(new[] { 1.0f, 2.5f, 0.5f }, vector.Values);
    }

    [Fact]
    public void SparseFloatVector_from_collections()
    {
        int[] indices = { 0, 5, 10 };
        float[] values = { 1.0f, 2.5f, 0.5f };
        var vector = new MilvusSparseVector<float>(indices, values);

        Assert.Equal(3, vector.Count);
        Assert.Equal(indices, vector.Indices);
        Assert.Equal(values, vector.Values);
    }

    [Fact]
    public void SparseFloatVector_negative_index_throws()
    {
        Assert.Throws<ArgumentException>(() => new MilvusSparseVector<float>(new Dictionary<int, float>
        {
            [-1] = 1.0f
        }));
    }

    [Fact]
    public void SparseFloatVector_nan_value_throws()
    {
        Assert.Throws<ArgumentException>(() => new MilvusSparseVector<float>(new Dictionary<int, float>
        {
            [0] = float.NaN
        }));
    }
}
