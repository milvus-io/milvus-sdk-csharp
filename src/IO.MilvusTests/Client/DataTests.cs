using IO.Milvus.Param;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.MilvusTests.Client.Base;
using IO.Milvus.Param.Dml;
using IO.MilvusTests;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Partition;

namespace IO.Milvus.Client.Tests
{
    /// <summary>
    /// a test class to execute unittest about data process
    /// </summary>
    [TestClass]
    public class DataTests : MilvusServiceClientTestsBase
    {
        public const string Book = nameof(Book);

        [TestMethod()]
        [DataRow(Book, HostConfig.DefaultTestPartitionName)]
        public void AInsertTest(string collectionName, string partitionName)
        {
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var data = PrepareData(collectionName, partitionName);

            var hasP = MilvusClient.HasPartition(HasPartitionParam.Create(collectionName, partitionName));
            if (!hasP.Data)
            {
                var createP = MilvusClient.CreatePartition(CreatePartitionParam.Create(collectionName, partitionName));
                AssertRpcStatus(createP);
            }

            var r = MilvusClient.Insert(data);

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.SuccIndex.Count > 0);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void BDeleteTest(string collectionName, string partitionName)
        {
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var hasP = MilvusClient.HasPartition(HasPartitionParam.Create(collectionName, partitionName));
            if (!hasP.Data)
            {
                var createP = MilvusClient.CreatePartition(CreatePartitionParam.Create(collectionName, partitionName));
                AssertRpcStatus(createP);
            }

            var data = PrepareData(collectionName, partitionName);
            var loadCollectionResult = MilvusClient.LoadCollection(new LoadCollectionParam
            {
                CollectionName = collectionName
            });

            var insertResult = MilvusClient.Insert(data);

            var r = MilvusClient.Delete(DeleteParam.Create(collectionName, $"bookIds > 2"));

            ThenDropCollection(collectionName);
            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.SuccIndex.Count > 0);
        }

        [TestMethod()]
        [DataRow(Book)]
        public void GetCompactionStateTest(string collectionName)
        {
            var r = MilvusClient.ManualCompaction(collectionName);

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());

            var stateR = MilvusClient.GetCompactionState(r.Data.CompactionID);
            Assert.IsNotNull(stateR);
            Assert.IsTrue(stateR.Status == Status.Success, stateR.Exception?.ToString());
        }
    }
}
