using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Dml;
using IO.Milvus.Param.Partition;
using IO.MilvusTests.Client.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IO.Milvus.Client.Tests
{
    [TestClass]
    public class QueryTest : MilvusServiceClientTestsBase
    {
        [TestMethod()]
        [DataRow("bookIds > 0 && bookIds < 2000")]
        public void QueryDataTest(string expr)
        {
            var collectionName = $"test{rd.Next()}";
            GivenNoCollection(collectionName);
            GivenCollection(collectionName);
            GivenBookIndex(collectionName);


            var partitionName = "_default";

            var hasP = MilvusClient.HasPartition(HasPartitionParam.Create(collectionName, partitionName));
            if (!hasP.Data)
            {
                var createP = MilvusClient.CreatePartition(CreatePartitionParam.Create(collectionName, partitionName));
                AssertRpcStatus(createP);
            }

            var loadCollectionResult = MilvusClient.LoadCollection(new LoadCollectionParam
            {
                CollectionName = collectionName
            });


            var data = PrepareData(collectionName, partitionName);
            var insertResult = MilvusClient.Insert(data);

            var r = MilvusClient.Query(QueryParam.Create(
                collectionName,
                new List<string> { partitionName },
                outFields: new List<string>() { "bookIds" },
                expr: expr));

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success);
        }
    }
}
