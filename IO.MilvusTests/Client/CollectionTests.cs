using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class CollectionTests : IAsyncLifetime
{
    [Fact]
    public async Task Create_Exists_and_Drop()
    {
        await Client.CreateCollectionAsync(
            CollectionName,
            new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) });
        Assert.True(await Client.HasCollectionAsync(CollectionName));

        await Client.DropCollectionAsync(CollectionName);
        Assert.False(await Client.HasCollectionAsync(CollectionName));
    }

    [Fact]
    public async Task Describe()
    {
        await Assert.ThrowsAsync<MilvusException>(() => Client.DescribeCollectionAsync(CollectionName));

        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("book_id", isPrimaryKey: true, autoId: true),
                FieldSchema.Create<bool>("is_cartoon", description: "Some cartoon"),
                FieldSchema.Create<sbyte>("chapter_count"),
                FieldSchema.Create<short>("short_page_count"),
                FieldSchema.Create<int>("int32_page_count"),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.Create<float>("float_weight"),
                FieldSchema.Create<double>("double_weight"),
                FieldSchema.CreateVarchar("book_name", maxLength: 256, isPartitionKey: true),
                FieldSchema.CreateFloatVector("book_intro", dimension: 2),
                FieldSchema.CreateJson("some_json")
            },
            shardsNum: 2,
            consistencyLevel: MilvusConsistencyLevel.Eventually);

        var collectionDescription = await Client.DescribeCollectionAsync(CollectionName);

        Assert.Equal(CollectionName, collectionDescription.CollectionName);
        Assert.Equal(2, collectionDescription.ShardsNum);
        Assert.Equal(MilvusConsistencyLevel.Eventually, collectionDescription.ConsistencyLevel);

        Assert.All(collectionDescription.Schema.Fields, f => Assert.Equal(MilvusFieldState.FieldCreated, f.State));

        Assert.Collection(collectionDescription.Schema.Fields,
            f =>
            {
                Assert.Equal("book_id", f.Name);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.True(f.IsPrimaryKey);
                Assert.True(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("is_cartoon", f.Name);
                Assert.Equal(MilvusDataType.Bool, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("Some cartoon", f.Description);
            },
            f =>
            {
                Assert.Equal("chapter_count", f.Name);
                Assert.Equal(MilvusDataType.Int8, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("short_page_count", f.Name);
                Assert.Equal(MilvusDataType.Int16, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("int32_page_count", f.Name);
                Assert.Equal(MilvusDataType.Int32, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("word_count", f.Name);
                Assert.Equal(MilvusDataType.Int64, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("float_weight", f.Name);
                Assert.Equal(MilvusDataType.Float, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("double_weight", f.Name);
                Assert.Equal(MilvusDataType.Double, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("book_name", f.Name);
                Assert.Equal(MilvusDataType.VarChar, f.DataType);
                Assert.Equal(256, f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.True(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("book_intro", f.Name);
                Assert.Equal(MilvusDataType.FloatVector, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Equal(2, f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            },
            f =>
            {
                Assert.Equal("some_json", f.Name);
                Assert.Equal(MilvusDataType.Json, f.DataType);
                Assert.Null(f.MaxLength);
                Assert.Null(f.Dimension);
                Assert.False(f.IsPrimaryKey);
                Assert.False(f.AutoId);
                Assert.False(f.IsPartitionKey);
                Assert.Equal("", f.Description);
            });
    }

    [Fact]
    public async Task Rename()
    {
        string renamedCollectionName = "RenamedCollection";

        await Client.DropCollectionAsync(renamedCollectionName);
        await Client.CreateCollectionAsync(
            CollectionName,
            new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) });

        await Client.RenameCollectionAsync(CollectionName, renamedCollectionName);

        Assert.False(await Client.HasCollectionAsync(CollectionName));
        Assert.True(await Client.HasCollectionAsync(renamedCollectionName));

        Assert.Equal(renamedCollectionName, (await Client.DescribeCollectionAsync(renamedCollectionName)).CollectionName);
    }

    [Fact]
    public async Task Load_Release()
    {
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await TestEnvironment.Client.CreateIndexAsync(
            CollectionName, "float_vector", MilvusIndexType.Flat,
            MilvusSimilarityMetricType.L2, new Dictionary<string, string>(), "float_vector_idx");

        await Client.LoadCollectionAsync(CollectionName);
        await TestEnvironment.Client.WaitForCollectionLoadAsync(CollectionName,
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        _ = await Client.QueryAsync(CollectionName, "id in [2, 3]", outputFields: new[] { "float_vector" });

        await Client.ReleaseCollectionAsync(CollectionName);

        await Assert.ThrowsAsync<MilvusException>(() =>
            Client.QueryAsync(CollectionName, "id in [2, 3]", outputFields: new[] { "float_vector" }));
    }

    [Fact]
    public async Task List()
    {
        await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });

        await TestEnvironment.Client.CreateIndexAsync(
            CollectionName, "float_vector", MilvusIndexType.Flat,
            MilvusSimilarityMetricType.L2, new Dictionary<string, string>(), "float_vector_idx");

        Assert.Single(await Client.ShowCollectionsAsync(), c => c.CollectionName == CollectionName);
        Assert.DoesNotContain(await Client.ShowCollectionsAsync(showType: ShowType.InMemory),
            c => c.CollectionName == CollectionName);

        await Client.LoadCollectionAsync(CollectionName);
        await TestEnvironment.Client.WaitForCollectionLoadAsync(CollectionName,
            waitingInterval: TimeSpan.FromMilliseconds(100), timeout: TimeSpan.FromMinutes(1));

        Assert.Single(await Client.ShowCollectionsAsync(), c => c.CollectionName == CollectionName);
        Assert.Single(await Client.ShowCollectionsAsync(showType: ShowType.InMemory),
            c => c.CollectionName == CollectionName);
    }

    [Fact]
    public async Task Create_in_non_existing_database_fails()
    {
        var exception = await Assert.ThrowsAsync<MilvusException>(() => Client.CreateCollectionAsync(
            "foo",
            new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) },
            dbName: "non_existing_db"));

        Assert.Equal("UnexpectedError", exception.ErrorCode);
        Assert.Equal("ErrorCode: UnexpectedError Reason: database:non_existing_db not found", exception.Message);
    }

    public Task InitializeAsync()
        => Client.DropCollectionAsync(CollectionName);

    public Task DisposeAsync()
        => Task.CompletedTask;

    private const string CollectionName = nameof(CollectionTests);
    private MilvusClient Client => TestEnvironment.Client;
}
