using FluentAssertions;
using IO.Milvus.Param.Dml;
using IO.Milvus.Param.Partition;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Helpers;
using Xunit;

namespace IO.MilvusTests.Client;

/// <summary>
/// a test class to execute unittest about data process
/// </summary>
public sealed class DataTests : MilvusServiceClientTestsBase, IAsyncLifetime
{
    private string collectionName;
    private string aliasName;
    private string partitionName;

    public async Task InitializeAsync()
    {
        collectionName = $"test{random.Next()}";

        aliasName = collectionName + "_aliasName";
        partitionName = collectionName + "_partitionName";
    }

    public async Task DisposeAsync()
    {
        this.ThenDropCollection(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public async Task AInsertTest()
    {
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);
        this.GivenPartition(collectionName, partitionName);

        var r = InsertDataToBookCollection(collectionName, partitionName);

        r.Assert();
    }

    [Fact]
    public async Task BDeleteTest()
    {
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var hasP = MilvusClient.HasPartition(HasPartitionParam.Create(collectionName, partitionName));
        if (!hasP.Data)
        {
            var createP = MilvusClient.CreatePartition(CreatePartitionParam.Create(collectionName, partitionName));
            createP.AssertRpcStatus();
        }

        InsertDataToBookCollection(collectionName, partitionName);

        var r = MilvusClient.Delete(DeleteParam.Create(collectionName, $"book_id in [2]"));

        r.ShouldSuccess();
        r.Data.DeleteCnt.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCompactionStateTest()
    {
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);
        InsertDataToBookCollection(collectionName, partitionName);

        var r = MilvusClient.ManualCompaction(collectionName);

        r.ShouldSuccess();

        var stateR = MilvusClient.GetCompactionState(r.Data.CompactionID);
        stateR.ShouldSuccess();
    }
}