using FluentAssertions;
using Xunit;

namespace Milvus.Client.Tests;

public partial class MilvusClientTests
{
    [Fact]
    public async Task SampleTest()
    {
        string collectionName = Client.GetType().Name;
        MilvusCollection collection = Client.GetCollection(collectionName);

        //Check if collection exist
        bool collectionExist = await Client.HasCollectionAsync(collectionName);
        if (collectionExist)
        {
            await collection.DropAsync();
            await Task.Delay(100);//avoid drop collection too frequently, cause error.
        }

        //Create collection
        await Client.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldSchema.Create<long>("book_id",isPrimaryKey:true),
                FieldSchema.Create<bool>("is_cartoon"),
                FieldSchema.Create<sbyte>("chapter_count"),
                FieldSchema.Create<short>("short_page_count"),
                FieldSchema.Create<int>("int32_page_count"),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.Create<float>("float_weight"),
                FieldSchema.Create<double>("double_weight"),
                FieldSchema.CreateVarchar("book_name",256),
                FieldSchema.CreateFloatVector("book_intro",2),}
            );
        collectionExist = await Client.HasCollectionAsync(collectionName);
        Assert.True(collectionExist, "Create collection failed");

        //Get collection info
        IDictionary<string, string> statistics = await collection.GetStatisticsAsync();
        Assert.True(statistics.Count == 1);
        MilvusCollectionDescription collectionDescription = await collection.DescribeAsync();
        Assert.Equal(collectionName, collectionDescription.CollectionName);

        //Check collection info
        collectionDescription.Schema.Name.Should().Be(collectionName);
        collectionDescription.Schema.Fields.Count.Should().Be(10);
        collectionDescription.Schema.Fields[0].Name.Should().Be("book_id");
        collectionDescription.ShardsNum.Should().Be(1);
        collectionDescription.Aliases.Should().BeNullOrEmpty();

        //Create alias
        string aliasName = "alias1";
        await Client.CreateAliasAsync(collectionName, aliasName);
        collectionDescription = await collection.DescribeAsync();
        collectionDescription.Aliases.First().Should().Be(aliasName);

        //TODO Create another collection to test alter alias.

        //Delete alias
        await Client.DropAliasAsync(aliasName);
        collectionDescription = await collection.DescribeAsync();
        collectionDescription.Aliases.Should().BeNullOrEmpty();

        //Create Partition
        string partitionName = "partition1";
        await collection.CreatePartitionAsync(partitionName);
        IList<MilvusPartition> partitions = await collection.ShowPartitionsAsync();
        partitions.Should().Contain(x => x.PartitionName == partitionName);

        IDictionary<string, string> collectionStatistics = await collection.GetStatisticsAsync();
        collectionStatistics.Should().ContainKey("row_count");

        //Insert data
        Random ran = new();
        List<long> bookIds = new();
        List<bool> isCartoon = new();
        List<sbyte> chapterCount = new();
        List<short> shortPageCount = new();
        List<int> int32PageCount = new();
        List<long> wordCounts = new();
        List<float> floatWeight = new();
        List<double> doubleWeight = new();
        List<ReadOnlyMemory<float>> bookIntros = new();
        List<string> bookNames = new();
        for (long i = 0L; i < 2000; ++i)
        {
            bookIds.Add(i);
            isCartoon.Add(i % 2 == 0);
            chapterCount.Add((sbyte)(i % 127));
            shortPageCount.Add((short)i);
            int32PageCount.Add((int)i);
            wordCounts.Add(i + 10000);
            floatWeight.Add(i + 0.1f);
            doubleWeight.Add(i + 0.1d);
            bookNames.Add($"Book Name {i}");

            float[] vector = new float[2];
            for (int k = 0; k < 2; ++k)
            {
                vector[k] = ran.Next();
            }
            bookIntros.Add(vector);
        }
        await collection.InsertAsync(
            new FieldData[]
            {
                FieldData.Create("book_id",bookIds),
                FieldData.Create("is_cartoon",isCartoon),
                FieldData.Create("chapter_count",chapterCount),
                FieldData.Create("short_page_count",shortPageCount),
                FieldData.Create("int32_page_count",int32PageCount),
                FieldData.Create("word_count",wordCounts),
                FieldData.Create("float_weight",floatWeight),
                FieldData.Create("double_weight",doubleWeight),
                FieldData.Create("book_name",bookNames),
                FieldData.CreateFloatVector("book_intro",bookIntros),},
            partitionName!);

        //Create index
        await collection.CreateIndexAsync(
            "book_intro",
            MilvusIndexType.IvfFlat,
            MilvusSimilarityMetricType.L2, new Dictionary<string, string> { { "nlist", "1024" } }, "idx");
        IList<MilvusIndex> indexes = await collection.DescribeIndexAsync("book_intro");
        indexes.Should().ContainSingle();
        indexes.First().IndexName.Should().Be("idx");
        indexes.First().FieldName.Should().Be("book_intro");

        //Load
        await collection.LoadPartitionsAsync(new List<string> { partitionName });

        //Wait loaded
        await collection.WaitForCollectionLoadAsync(
            string.IsNullOrEmpty(partitionName) ? null : new[] { partitionName },
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(20));

        //Search
        var searchResult = await collection.SearchAsync(
            vectorFieldName: "book_intro",
            new ReadOnlyMemory<float>[] { new[] { 0.1f, 0.2f } },
            MilvusSimilarityMetricType.L2,
            limit: 2,
            new()
            {
                OutputFields = { "book_id" },
                ConsistencyLevel = ConsistencyLevel.Strong,
                Parameters = { { "nprobe", "10" }, { "offset", "5" } },
            });

        searchResult.FieldsData.Should().ContainSingle();

        //Query
        string expr = "book_id in [2,4,6,8]";
        var queryResult = await collection.QueryAsync(
            expr,
            new[] { "book_id", "word_count", "book_intro" });
        queryResult.FieldsData.Count.Should().Be(3);
        Assert.All(queryResult.FieldsData, p => Assert.Equal(4, p.RowCount));

        //Delete
        MilvusMutationResult deleteResult = await collection.DeleteAsync("book_id in [0,1]", partitionName);
        deleteResult.DeleteCount.Should().BeGreaterThan(0);

        //Compaction
        long compactionId = await Client.ManualCompactionAsync(collectionDescription.CollectionId); ;
        compactionId.Should().NotBe(0);
        MilvusCompactionState state = await Client.GetCompactionStateAsync(compactionId);

        //Release
        await collection.ReleasePartitionAsync(new[] { partitionName! });

        //Drop index
        await collection.DropIndexAsync("book_intro", "idx");
        await Assert.ThrowsAsync<MilvusException>(async () => await collection.DescribeIndexAsync("book_intro"));

        //Drop partition
        await collection.DropPartitionsAsync(partitionName!);
        partitions = await collection.ShowPartitionsAsync();
        partitions.Should().NotContain(p => p.PartitionName == partitionName);

        //Drop collection
        await collection.DropAsync();
        //Check if collection exist
        collectionExist = await Client.HasCollectionAsync(collectionName);
        collectionExist.Should().BeFalse("Collection delete failed");
    }

    private MilvusClient Client => TestEnvironment.Client;
}
