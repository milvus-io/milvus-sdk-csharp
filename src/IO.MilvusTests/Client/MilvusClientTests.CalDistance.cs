using FluentAssertions;
using IO.Milvus;
using IO.Milvus.Client;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task CalDistanceTest(IMilvusClient milvusClient)
    {
        if (milvusClient.IsZillizCloud())
        {
            return;
        }

        string collectionName = milvusClient.GetType().Name;

        await milvusClient.CreateBookCollectionAndIndex(collectionName);
        await milvusClient.WaitLoadedAsync(collectionName);

        var vectorsLeft = MilvusVectors.CreateIds(
            collectionName, 
            "book_intro",
            new long[] {1,2});

        var vectorsRight = MilvusVectors.CreateFloatVectors(
                new List<List<float>> {
                    new List<float> { 1,2},
                    new List<float> { 3,4},
                    new List<float> { 5,6},
                    new List<float> { 7,8},
                }
            );

        var result = await milvusClient.CalDistanceAsync(vectorsLeft, vectorsRight, MilvusMetricType.IP);

        result.FloatDistance.Should().NotBeNullOrEmpty();
        result.FloatDistance.Count.Should().Be(8);
    }
}
