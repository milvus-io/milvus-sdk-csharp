using FluentAssertions;
using IO.Milvus.Grpc;
using IO.Milvus.Param.Partition;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Helpers;
using Xunit;
using Status = IO.Milvus.Param.Status;

namespace IO.MilvusTests.Client;

public sealed class PartitionTest : MilvusServiceClientTestsBase, IAsyncLifetime
{
    private string collectionName;
    private string aliasName;
    private string partitionName;

    public async Task InitializeAsync()
    {
        collectionName = $"test{random.Next()}";

        aliasName = collectionName + "_aliasName";
        partitionName = collectionName + "_partitionName";

        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);
        InsertDataToBookCollection(collectionName, partitionName);
    }

    public async Task DisposeAsync()
    {
        this.ThenDropCollection(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public void ACreatePartitionTest()
    {
        var r = MilvusClient.CreatePartition(CreatePartitionParam.Create(
            collectionName, partitionName));

        r.AssertRpcStatus();
    }

    [Fact]
    public void BLoadPartitionsTest()
    {
        this.GivenPartition(collectionName, partitionName);

        var r = MilvusClient.LoadPartitions(LoadPartitionsParam.Create(
            collectionName, partitionName));

        r.AssertRpcStatus();
    }

    [Fact]
    public void CHasPartitionTest()
    {
        this.GivenPartition(collectionName, partitionName);

        var r = MilvusClient.HasPartition(HasPartitionParam.Create(
            collectionName, partitionName));

        r.Status.Should().Be(Status.Success);
        r.Data.Should().BeTrue();
    }

    [Fact]
    public void DGetPartitionStatisticsTest()
    {
        this.GivenPartition(collectionName, partitionName);

        var r = MilvusClient.GetPartitionStatistics(GetPartitionStatisticsParam.Create(
            collectionName, partitionName));

        r.ShouldSuccess();
        r.Data.Stats.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EShowPartitionsTest()
    {
        this.GivenPartition(collectionName, partitionName);

        var r = MilvusClient.ShowPartitions(ShowPartitionsParam.Create(
            collectionName, null));

        r.ShouldSuccess();
        r.Data.Status.ErrorCode.Should().Be(ErrorCode.Success);
    }

    [Fact]
    public void FReleasePartitionsTest()
    {
        this.GivenPartition(collectionName, partitionName);

        var r = MilvusClient.ReleasePartitions(ReleasePartitionsParam.Create(
            collectionName, partitionName));

        r.AssertRpcStatus();
    }

    [Fact]
    public void GDropPartitionTest()
    {
        this.GivenPartition(collectionName, partitionName);

        var r = MilvusClient.DropPartition(DropPartitionParam.Create(
            collectionName, partitionName));

        r.AssertRpcStatus();
    }
}