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
}
