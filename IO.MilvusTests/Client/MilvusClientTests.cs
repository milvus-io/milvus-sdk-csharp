using FluentAssertions;
using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Fact]
    public async Task SampleTest()
    {
        string collectionName = Client.GetType().Name;

        //Check if collection exist
        bool collectionExist = await Client.HasCollectionAsync(collectionName);
        if (collectionExist)
        {
            await Client.DropCollectionAsync(collectionName);
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
        IDictionary<string, string> statistics = await Client.GetCollectionStatisticsAsync(collectionName);
        Assert.True(statistics.Count == 1);
        MilvusCollectionDescription collectionDescription = await Client.DescribeCollectionAsync(collectionName);
        Assert.Equal(collectionName, collectionDescription.CollectionName);

        //Check collection info
        collectionDescription.Schema.Name.Should().Be(collectionName);
        collectionDescription.Schema.Fields.Count.Should().Be(10);
        collectionDescription.Schema.Fields[0].Name.Should().Be("book_id");
        collectionDescription.ShardsNum.Should().Be(1);
        collectionDescription.Aliases.Should().BeNullOrEmpty();
        string? partitionName = TestEnvironment.IsZillizCloud ? null : "partition1";
        if (!TestEnvironment.IsZillizCloud)
        {
            //Create alias
            string aliasName = "alias1";
            await Client.CreateAliasAsync(collectionName, aliasName);
            collectionDescription = await Client.DescribeCollectionAsync(collectionName);
            collectionDescription.Aliases.First().Should().Be(aliasName);

            //TODO Create another collection to test alter alias.

            //Delete alias
            await Client.DropAliasAsync(aliasName);
            collectionDescription = await Client.DescribeCollectionAsync(collectionName);
            collectionDescription.Aliases.Should().BeNullOrEmpty();

            //Create Partition
            await Client.CreatePartitionAsync(collectionName, partitionName!);
            IList<MilvusPartition> partitions = await Client.ShowPartitionsAsync(collectionName);
            partitions.Should().Contain(x => x.PartitionName == partitionName);
        }
        IDictionary<string, string> collectionStatistics = await Client.GetCollectionStatisticsAsync(collectionName);
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
        List<List<float>> bookIntros = new();
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

            List<float> vector = new();
            for (int k = 0; k < 2; ++k)
            {
                vector.Add(ran.Next());
            }
            bookIntros.Add(vector);
        }
        await Client.InsertAsync(collectionName,
            new Field[]
            {
                Field.Create("book_id",bookIds),
                Field.Create("is_cartoon",isCartoon),
                Field.Create("chapter_count",chapterCount),
                Field.Create("short_page_count",shortPageCount),
                Field.Create("int32_page_count",int32PageCount),
                Field.Create("word_count",wordCounts),
                Field.Create("float_weight",floatWeight),
                Field.Create("double_weight",doubleWeight),
                Field.Create("book_name",bookNames),
                Field.CreateFloatVector("book_intro",bookIntros),},
            partitionName!);

        //Create index
        await Client.CreateIndexAsync(
            collectionName,
            "book_intro",
            Constants.DefaultIndexName,
            TestEnvironment.IsZillizCloud ? MilvusIndexType.AutoIndex : MilvusIndexType.IvfFlat,
            MilvusMetricType.L2,
            new Dictionary<string, string> { { "nlist", "1024" } });
        IList<MilvusIndex> indexes = await Client.DescribeIndexAsync(collectionName, "book_intro");
        indexes.Should().ContainSingle();
        indexes.First().IndexName.Should().Be(Constants.DefaultIndexName);
        indexes.First().FieldName.Should().Be("book_intro");

        //Load
        if (TestEnvironment.IsZillizCloud)
        {
            await Client.LoadCollectionAsync(collectionName);
        }
        else if (partitionName != null)
        {
            await Client.LoadPartitionsAsync(collectionName, new List<string> { partitionName });
        }

        //Wait loaded
        await Client.WaitForCollectionLoadAsync(
            collectionName,
            string.IsNullOrEmpty(partitionName) ? null : new[] { partitionName },
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(20));

        //Search
        List<string> search_output_fields = new() { "book_id" };
        List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
        var searchResult = await Client.SearchAsync(
            MilvusSearchParameters.Create(collectionName, "book_intro", search_output_fields)
            .WithVectors(search_vectors)
            .WithConsistencyLevel(MilvusConsistencyLevel.Strong)
            .WithMetricType(MilvusMetricType.L2)
            .WithTopK(topK: 2)
            .WithParameter("nprobe", "10")
            .WithParameter("offset", "5"));
        searchResult.Results.FieldsData.Should().ContainSingle();

        //Query
        string expr = "book_id in [2,4,6,8]";
        var queryResult = await Client.QueryAsync(
            collectionName,
            expr,
            new[] { "book_id", "word_count", "book_intro" });
        queryResult.FieldsData.Count.Should().Be(3);
        Assert.All(queryResult.FieldsData, p => Assert.Equal(4, p.RowCount));

        //Delete
        MilvusMutationResult deleteResult = await Client.DeleteAsync(collectionName, "book_id in [0,1]", partitionName);
        deleteResult.DeleteCount.Should().BeGreaterThan(0);

        //Compaction
        long compactionId = await Client.ManualCompactionAsync(collectionDescription.CollectionId); ;
        compactionId.Should().NotBe(0);
        MilvusCompactionState state = await Client.GetCompactionStateAsync(compactionId);

        //Release
        if (TestEnvironment.IsZillizCloud)
        {
            await Client.ReleaseCollectionAsync(collectionName);
        }
        else
        {
            await Client.ReleasePartitionAsync(collectionName, new[] { partitionName! });
        }

        //Drop index
        await Client.DropIndexAsync(collectionName, "book_intro", Constants.DefaultIndexName);
        await Assert.ThrowsAsync<MilvusException>(async () => await Client.DescribeIndexAsync(collectionName, "book_intro"));

        //Drop partition
        if (!TestEnvironment.IsZillizCloud)
        {
            //Drop partition
            await Client.DropPartitionsAsync(collectionName, partitionName!);
            IList<MilvusPartition> partitions = await Client.ShowPartitionsAsync(collectionName);
            partitions.Should().NotContain(p => p.PartitionName == partitionName);
        }

        //Drop collection
        await Client.DropCollectionAsync(collectionName);
        //Check if collection exist
        collectionExist = await Client.HasCollectionAsync(collectionName);
        collectionExist.Should().BeFalse("Collection delete failed");
    }

    private MilvusClient Client => TestEnvironment.Client;
}
