using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Diagnostics;
using Xunit;

namespace IO.MilvusTests.Client;

public class CollectionTests
{
    [Fact]
    public async Task Create_Exists_and_Drop()
    {
        var collectionName = nameof(Create_Exists_and_Drop);

        await Client.DropCollectionAsync(collectionName);
        Assert.False(await Client.HasCollectionAsync(collectionName));

        await Client.CreateCollectionAsync(
            collectionName,
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        Assert.True(await Client.HasCollectionAsync(collectionName));

        await Client.DropCollectionAsync(collectionName);
        Assert.False(await Client.HasCollectionAsync(collectionName));
    }

    [Fact]
    public async Task List()
    {
        var collectionName = nameof(List);

        await Client.DropCollectionAsync(collectionName);
        await Client.CreateCollectionAsync(
            collectionName,
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        Assert.Single(await Client.ShowCollectionsAsync(), c => c.CollectionName == collectionName);
    }

    [Fact]
    public async Task Describe()
    {
        var collectionName = nameof(Describe);

        await Client.DropCollectionAsync(collectionName);

        await Assert.ThrowsAsync<MilvusException>(() => Client.DescribeCollectionAsync(collectionName));

        await Client.CreateCollectionAsync(
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

        var collectionDescription = await Client.DescribeCollectionAsync(collectionName);

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

    [Fact]
    public async Task Rename()
    {
        var oldCollectionName = nameof(Rename);
        var newCollectionName = oldCollectionName + "New";

        await Client.DropCollectionAsync(oldCollectionName);
        await Client.DropCollectionAsync(newCollectionName);
        await Client.CreateCollectionAsync(
            oldCollectionName,
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        await Client.RenameCollectionAsync(oldCollectionName, newCollectionName);

        Assert.False(await Client.HasCollectionAsync(oldCollectionName));
        Assert.True(await Client.HasCollectionAsync(newCollectionName));

        Assert.Equal(newCollectionName, (await Client.DescribeCollectionAsync(newCollectionName)).CollectionName);
    }

    // TODO: Load
    // TODO: Release

    private MilvusClient Client => TestEnvironment.Client;
}
