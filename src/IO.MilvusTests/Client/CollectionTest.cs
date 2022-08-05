using IO.Milvus.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests;

namespace IO.Milvus.Client.Tests
{
    [TestClass]
    public class CollectionTest:MilvusServiceClientTestsBase
    {
        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, true)]
        [DataRow("test2", false)]
        public void HasCollectionTest(string collectionName, bool exist)
        {
            var r = MilvusClient.HasCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success);
            Assert.IsTrue(r.Data == exist);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public void HasCollectionErrorTest(string collectionName)
        {
            var r = MilvusClient.HasCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success,r.Exception?.ToString());
        }

        [TestMethod()]
        public void CreateCollectionTest()
        {
            var rd = new Random(DateTime.Now.Second);
            var collectionName = $"test{rd.Next()}";

            var r = MilvusClient.CreateCollection(
                CreateCollectionParam.Create(
                   collectionName: collectionName,
                   shardsNum: 2,
                   new List<FieldType>()
                {
                    FieldType.Create(
                        $"priKey{rd.Next()}",
                    Grpc.DataType.Int64,
                    null,
                    true
                    )
                }));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsNotNull(r.Data);

            var hasR = MilvusClient.HasCollection(HasCollectionParam.Create(collectionName));
            Assert.IsTrue(hasR.Status == Status.Success);
            Assert.IsTrue(hasR.Data);

            MilvusClient.DropCollection(collectionName);

            hasR = MilvusClient.HasCollection(HasCollectionParam.Create(collectionName));
            Assert.IsTrue(hasR.Status == Status.Success);
            Assert.IsTrue(!hasR.Data);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public void ShowCollectionsTest(string collectionName)
        {
            var r = MilvusClient.ShowCollections(ShowCollectionsParam.Create(null));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.CollectionNames.Contains(collectionName));
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public void DescribeCollectionTest(string collectionName)
        {
            var r = MilvusClient.DescribeCollection(DescribeCollectionParam.Create(collectionName));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Schema.Name == collectionName);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public void DescribeCollectionWithNameTest(string collectionName)
        {
            var r = MilvusClient.DescribeCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Schema.Name == collectionName);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, true)]
        [DataRow(HostConfig.DefaultTestCollectionName, false)]
        public void GetCollectionStatisticsTest(string collectionName,bool isFlushCollection)
        {
            var r = MilvusClient.GetCollectionStatistics(GetCollectionStatisticsParam.Create(collectionName,isFlushCollection));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Stats.First().Key == "row_count");
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public void LoadCollectionTest(string collectionName)
        {
            var r = MilvusClient.LoadCollection(LoadCollectionParam.Create(collectionName));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            r = MilvusClient.ReleaseCollection(ReleaseCollectionParam.Create(collectionName));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public void LoadCollectionWithNameTest(string collectionName)
        {
            var r = MilvusClient.LoadCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            r = MilvusClient.ReleaseCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);
        }

        //[TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public async Task LoadCollectionAsyncTestAsync(string collectionName)
        {
            var r = await MilvusClient.LoadCollectionAsync(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            r = await MilvusClient.ReleaseCollectionAsync(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);
        }

        //[TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName)]
        public async Task LoadCollectionAsyncWithNameTestAsync(string collectionName)
        {
            var r = await MilvusClient.LoadCollectionAsync(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            r = await MilvusClient.ReleaseCollectionAsync(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);
        }
    }
}
