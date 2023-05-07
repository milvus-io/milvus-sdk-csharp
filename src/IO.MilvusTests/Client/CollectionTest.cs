using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests;

namespace IO.Milvus.Client.Tests
{
    [TestClass]
    public class CollectionTest : MilvusServiceClientTestsBase
    {
        [TestMethod]
        [DataRow(HostConfig.DefaultTestCollectionName, true)]
        [DataRow("test2", false)]
        public void HasCollectionTest(string collectionName, bool exist)
        {
            var r = MilvusClient.HasCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success);
            Assert.IsTrue(r.Data == exist);
        }

        [TestMethod]
        public void HasCollectionErrorTest()
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.HasCollection(collectionName);

            ThenDropCollection(collectionName);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
        }

        [TestMethod]
        public void CreateCollectionTest()
        {
            var collectionName = $"test{rd.Next()}";
            var r = CreateBookCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsNotNull(r.Data);

            var hasR = MilvusClient.HasCollection(HasCollectionParam.Create(collectionName));
            Assert.IsTrue(hasR.Status == Status.Success);
            Assert.IsTrue(hasR.Data);

            ThenDropCollection(collectionName);

            hasR = MilvusClient.HasCollection(HasCollectionParam.Create(collectionName));
            Assert.IsTrue(hasR.Status == Status.Success);
            Assert.IsTrue(!hasR.Data);
        }

        [TestMethod]
        public void ShowCollectionsTest()
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.ShowCollections(ShowCollectionsParam.Create(null));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.CollectionNames.Contains(collectionName));

            ThenDropCollection(collectionName);
        }

        [TestMethod]
        public void DescribeCollectionTest()
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.DescribeCollection(DescribeCollectionParam.Create(collectionName));

            ThenDropCollection(collectionName);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Schema.Name == collectionName);
        }

        [TestMethod]
        public void DescribeCollectionWithNameTest()
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.DescribeCollection(collectionName);

            ThenDropCollection(collectionName);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Schema.Name == collectionName);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCollectionStatisticsTest(bool isFlushCollection)
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.GetCollectionStatistics(
                GetCollectionStatisticsParam.Create(collectionName, isFlushCollection));

            ThenDropCollection(collectionName);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Stats.First().Key == "row_count");
        }

        [TestMethod]
        public void LoadCollectionTest()
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.LoadCollection(LoadCollectionParam.Create(collectionName));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            r = MilvusClient.ReleaseCollection(ReleaseCollectionParam.Create(collectionName));

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);


            ThenDropCollection(collectionName);
        }

        [TestMethod]
        public void LoadCollectionWithNameTest()
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);

            var r = MilvusClient.LoadCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            r = MilvusClient.ReleaseCollection(collectionName);

            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);

            ThenDropCollection(collectionName);
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
