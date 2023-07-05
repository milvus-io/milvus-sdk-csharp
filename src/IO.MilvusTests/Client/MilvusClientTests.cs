using FluentAssertions;
using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task SampleTest(IMilvusClient milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        //Check if collection exist
        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
            await Task.Delay(100);//avoid drop collection too frequently, cause error.
        }

        //Create collection
        await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                FieldType.Create<long>("book_id",isPrimaryKey:true),
                FieldType.Create<bool>("is_cartoon"),
                FieldType.Create<sbyte>("chapter_count"),
                FieldType.Create<short>("short_page_count"),
                FieldType.Create<int>("int32_page_count"),
                FieldType.Create<long>("word_count"),
                FieldType.Create<float>("float_weight"),
                FieldType.Create<double>("double_weight"),
                FieldType.CreateVarchar("book_name",256),
                FieldType.CreateFloatVector("book_intro",2),}
            );
        collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.True(collectionExist, "Create collection failed");

        //Get collection info
        IDictionary<string, string> statistics = await milvusClient.GetCollectionStatisticsAsync(collectionName);
        Assert.True(statistics.Count == 1);
        DetailedMilvusCollection detailedMilvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.Equal(collectionName, detailedMilvusCollection.CollectionName);

        //Check collection info
        detailedMilvusCollection.Schema.Name.Should().Be(collectionName);
        detailedMilvusCollection.Schema.Fields.Count.Should().Be(10);
        detailedMilvusCollection.Schema.Fields[0].Name.Should().Be("book_id");
        detailedMilvusCollection.ShardsNum.Should().Be(1);
        detailedMilvusCollection.Aliases.Should().BeNullOrEmpty();
        string? partitionName = milvusClient.IsZillizCloud() ? null : "partition1";
        if (!milvusClient.IsZillizCloud())
        {
            //Create alias
            string aliasName = "alias1";
            await milvusClient.CreateAliasAsync(collectionName, aliasName);
            detailedMilvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
            detailedMilvusCollection.Aliases.First().Should().Be(aliasName);

            //TODO Create another collection to test alter alias.

            //Delete alias
            await milvusClient.DropAliasAsync(aliasName);
            detailedMilvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
            detailedMilvusCollection.Aliases.Should().BeNullOrEmpty();

            //Create Partition
            await milvusClient.CreatePartitionAsync(collectionName, partitionName);
            IList<MilvusPartition> partitions = await milvusClient.ShowPartitionsAsync(collectionName);
            partitions.Should().Contain(x => x.PartitionName == partitionName);
        }
        IDictionary<string, string> collectionStatistics = await milvusClient.GetCollectionStatisticsAsync(collectionName);
        collectionStatistics.Should().ContainKey("row_count");

        //Insert data
        Random ran = new Random();
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
        await milvusClient.InsertAsync(collectionName,
            new Field[]
            {
                Field.Create<long>("book_id",bookIds),
                Field.Create<bool>("is_cartoon",isCartoon),
                Field.Create<sbyte>("chapter_count",chapterCount),
                Field.Create<short>("short_page_count",shortPageCount),
                Field.Create<int>("int32_page_count",int32PageCount),
                Field.Create<long>("word_count",wordCounts),
                Field.Create<float>("float_weight",floatWeight),
                Field.Create<double>("double_weight",doubleWeight),
                Field.Create<string>("book_name",bookNames),
                Field.CreateFloatVector("book_intro",bookIntros),},
            partitionName);

        //Create index
        await milvusClient.CreateIndexAsync(
            collectionName,
            "book_intro",
            Constants.DEFAULT_INDEX_NAME,
            milvusClient.IsZillizCloud() ? MilvusIndexType.AUTOINDEX : MilvusIndexType.IVF_FLAT,
            MilvusMetricType.L2,
            new Dictionary<string, string> { { "nlist", "1024" } });
        IList<MilvusIndex> indexes = await milvusClient.DescribeIndexAsync(collectionName, "book_intro");
        indexes.Should().ContainSingle();
        indexes.First().IndexName.Should().Be(Constants.DEFAULT_INDEX_NAME);
        indexes.First().FieldName.Should().Be("book_intro");

        //Load
        if (milvusClient.IsZillizCloud())
        {
            await milvusClient.LoadCollectionAsync(collectionName);
        }
        else if (partitionName != null)
        {
            await milvusClient.LoadPartitionsAsync(collectionName, new List<string> { partitionName });
        }

        //Wait loaded
        if (milvusClient is MilvusRestClient milvusRestClient)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
        else
        {
            await milvusClient.WaitForCollectionLoadAsync(
                collectionName,
                string.IsNullOrEmpty(partitionName) ? null : new[] { partitionName },
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(20));
        }

        //Search
        List<string> search_output_fields = new() { "book_id" };
        List<List<float>> search_vectors = new() { new() { 0.1f, 0.2f } };
        var searchResult = await milvusClient.SearchAsync(
            MilvusSearchParameters.Create(collectionName, "book_intro", search_output_fields)
            .WithVectors(search_vectors)
            .WithConsistencyLevel(MilvusConsistencyLevel.Strong)
            .WithMetricType(MilvusMetricType.L2)
            .WithTopK(topK: 2)
            .WithParameter("nprobe", "10")
            .WithParameter("offset", "5")
            );
        searchResult.Results.FieldsData.Should().ContainSingle();

        //Query
        string expr = "book_id in [2,4,6,8]";
        var queryResult = await milvusClient.QueryAsync(
            collectionName,
            expr,
            new[] { "book_id", "word_count", "book_intro" });
        queryResult.FieldsData.Count.Should().Be(3);
        Assert.All(queryResult.FieldsData, p => Assert.Equal(4, p.RowCount));

        //Cal distance
        if (!milvusClient.IsZillizCloud())
        {
            var vectorsLeft = MilvusVectors.CreateIds(
                collectionName,
                "book_intro",
                new long[] { 1, 2 });
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

        //Delete
        MilvusMutationResult deleteResult = await milvusClient.DeleteAsync(collectionName, "book_id in [0,1]", partitionName);
        deleteResult.DeleteCount.Should().BeGreaterThan(0);

        //Compaction
        long compactionId = await milvusClient.ManualCompactionAsync(detailedMilvusCollection.CollectionId); ;
        compactionId.Should().NotBe(0);
        MilvusCompactionState state = await milvusClient.GetCompactionStateAsync(compactionId);

        //Release
        if (milvusClient.IsZillizCloud())
        {
            await milvusClient.ReleaseCollectionAsync(collectionName);
        }
        else
        {
            await milvusClient.ReleasePartitionAsync(collectionName, new[] { partitionName });
        }

        //Drop index
        await milvusClient.DropIndexAsync(collectionName, "book_intro", Constants.DEFAULT_INDEX_NAME);
        await Assert.ThrowsAsync<MilvusException>(async () => await milvusClient.DescribeIndexAsync(collectionName, "book_intro"));

        //Drop partition
        if (!milvusClient.IsZillizCloud())
        {
            //Drop partition
            await milvusClient.DropPartitionsAsync(collectionName, partitionName);
            IList<MilvusPartition> partitions = await milvusClient.ShowPartitionsAsync(collectionName);
            partitions.Should().NotContain(p => p.PartitionName == partitionName);
        }

        //Drop collection
        await milvusClient.DropCollectionAsync(collectionName);
        //Check if collection exist
        collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        collectionExist.Should().BeFalse("Collection delete failed");
    }
}
