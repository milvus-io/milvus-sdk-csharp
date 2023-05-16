using IO.Milvus.Grpc;
using IO.Milvus.Param.Partition;
using IO.MilvusTests.Client.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Status = IO.Milvus.Param.Status;

namespace IO.MilvusTests.Client
{
    [TestClass()]
    public class PartitionTest:MilvusServiceClientTestsBase
    {
        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName,HostConfig.DefaultTestPartitionName)]
        public void ACreatePartitionTest(string collectionName,string partitionName)
        {
            var r = MilvusClient.CreatePartition(CreatePartitionParam.Create(
                collectionName, partitionName));

            AssertRpcStatus(r);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void BLoadPartitionsTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.LoadPartitions(LoadPartitionsParam.Create(
                collectionName, partitionName));

            AssertRpcStatus(r);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void CHasPartitionTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.HasPartition(HasPartitionParam.Create(
                collectionName, partitionName));

            AssertRBool(r);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void DGetPartitionStatisticsTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.GetPartitionStatistics(GetPartitionStatisticsParam.Create(
                        collectionName, partitionName));

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Stats.Count > 0);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void EShowPartitionsTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.ShowPartitions(ShowPartitionsParam.Create(
            collectionName,null));

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Status.ErrorCode == ErrorCode.Success);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void FReleasePartitionsTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.ReleasePartitions(ReleasePartitionsParam.Create(
                collectionName, partitionName));

            AssertRpcStatus(r);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void GDropPartitionTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.DropPartition(DropPartitionParam.Create(
                    collectionName, partitionName));

            AssertRpcStatus(r);
        }

    }
}
