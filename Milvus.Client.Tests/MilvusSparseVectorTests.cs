using Xunit;

namespace Milvus.Client.Tests;

public class MilvusSparseVectorTests
{
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
    public void SparseVector_Equals()
    {
        var v1 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v2 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v3 = new MilvusSparseVector<float>((int[])[0, 101], (float[])[1.0f, 2.0f]);
        var v4 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 3.0f]);
        var v5 = new MilvusSparseVector<float>((int[])[0], (float[])[1.0f]);

        Assert.True(v1.Equals(v2));
        Assert.True(v1 == v2);
        Assert.False(v1 != v2);
        Assert.True(v1.Equals((object)v2));

        Assert.False(v1.Equals(v3));
        Assert.False(v1 == v3);
        Assert.True(v1 != v3);

        Assert.False(v1.Equals(v4));
        Assert.False(v1.Equals(v5));
        Assert.False(v1.Equals(null));
        Assert.False(v1.Equals("not a vector"));
    }

    [Fact]
    public void SparseVector_GetHashCode()
    {
        var v1 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v2 = new MilvusSparseVector<float>((int[])[0, 100], (float[])[1.0f, 2.0f]);
        var v3 = new MilvusSparseVector<float>((int[])[0, 101], (float[])[1.0f, 2.0f]);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }
}
