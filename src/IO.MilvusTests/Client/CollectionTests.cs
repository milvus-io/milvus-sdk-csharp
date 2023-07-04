using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using Xunit;

namespace IO.MilvusTests.Client;

public class CollectionTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Create_Exists_and_Drop(IMilvusClient client)
    {
        var collectionName = nameof(Create_Exists_and_Drop);

        await client.DropCollectionAsync(collectionName);
        Assert.False(await client.HasCollectionAsync(collectionName));

        await client.CreateCollectionAsync(
            collectionName,
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        Assert.True(await client.HasCollectionAsync(collectionName));

        await client.DropCollectionAsync(collectionName);
        Assert.False(await client.HasCollectionAsync(collectionName));
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task List(IMilvusClient client)
    {
        var collectionName = nameof(List);

        await client.DropCollectionAsync(collectionName);
        await client.CreateCollectionAsync(
            collectionName,
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        Assert.Single(await client.ShowCollectionsAsync(), c => c.CollectionName == collectionName);
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Describe(IMilvusClient client)
    {
        var collectionName = nameof(Describe);

        await client.DropCollectionAsync(collectionName);

        await Assert.ThrowsAsync<MilvusException>(() => client.DescribeCollectionAsync(collectionName));

        await client.CreateCollectionAsync(
            collectionName,
            new[]
            {
                FieldType.Create<long>("book_id", isPrimaryKey: true),
                FieldType.Create<bool>("is_cartoon"),
                FieldType.Create<sbyte>("chapter_count"),
                FieldType.Create<short>("short_page_count"),
                FieldType.Create<int>("int32_page_count"),
                FieldType.Create<long>("word_count"),
                FieldType.Create<float>("float_weight"),
                FieldType.Create<double>("double_weight"),
                FieldType.CreateVarchar("book_name", 256),
                FieldType.CreateFloatVector("book_intro", 2)
            },
            shardsNum: 2,
            consistencyLevel: MilvusConsistencyLevel.Eventually);

        var collectionDescription = await client.DescribeCollectionAsync(collectionName);

        Assert.Equal(collectionName, collectionDescription.CollectionName);
        Assert.Equal(2, collectionDescription.ShardsNum);
        Assert.Equal(MilvusConsistencyLevel.Eventually, collectionDescription.ConsistencyLevel);

        Assert.Collection(collectionDescription.Schema.Fields,
            f =>
            {
                Assert.Equal("book_id", f.Name);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.True(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("is_cartoon", f.Name);
                Assert.Equal(MilvusDataType.Bool, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("chapter_count", f.Name);
                Assert.Equal(MilvusDataType.Int8, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("short_page_count", f.Name);
                Assert.Equal(MilvusDataType.Int16, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("int32_page_count", f.Name);
                Assert.Equal(MilvusDataType.Int32, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("word_count", f.Name);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("float_weight", f.Name);
                Assert.Equal(MilvusDataType.Float, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("double_weight", f.Name);
                Assert.Equal(MilvusDataType.Double, f.DataType);
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("book_name", f.Name);
                Assert.Equal(MilvusDataType.VarChar, f.DataType);
                // TODO: Assert max length
                Assert.False(f.IsPrimaryKey);
            },
            f =>
            {
                Assert.Equal("book_intro", f.Name);
                Assert.Equal(MilvusDataType.FloatVector, f.DataType);
                // TODO: Assert dim
                Assert.False(f.IsPrimaryKey);
            });
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Rename(IMilvusClient client)
    {
        if (client is MilvusRestClient)
        {
            return; // Collection rename not supported over REST
        }

        var oldCollectionName = nameof(Rename);
        var newCollectionName = oldCollectionName + "New";

        await client.DropCollectionAsync(oldCollectionName);
        await client.DropCollectionAsync(newCollectionName);
        await client.CreateCollectionAsync(
            oldCollectionName,
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        await client.RenameCollectionAsync(oldCollectionName, newCollectionName);

        Assert.False(await client.HasCollectionAsync(oldCollectionName));
        Assert.True(await client.HasCollectionAsync(newCollectionName));

        Assert.Equal(newCollectionName, (await client.DescribeCollectionAsync(newCollectionName)).CollectionName);
    }

    // TODO: Load
    // TODO: Release
}
