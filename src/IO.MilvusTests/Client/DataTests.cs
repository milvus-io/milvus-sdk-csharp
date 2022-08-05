using IO.Milvus.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.MilvusTests.Client.Base;
using IO.Milvus.Param.Dml;
using Google.Protobuf.Collections;
using IO.Milvus.Grpc;
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

        private InsertParam PreareData(string collectionName, string partitionName)
        {
            var r = new Random(DateTime.Now.Second);

            var bookIds = new List<long>();
            var wordCounts = new List<long>();
            var bookIntros = new List<List<float>>();

            for (int i = 0; i < 2000; i++)
            {
                bookIds.Add(i);
                wordCounts.Add(i + 10000);
                var vector = new List<float>();
                for (int k = 0; k < 2; ++k)
                {
                    vector.Add(r.NextSingle());
                }
                bookIntros.Add(vector);
            }

            var insertParam = InsertParam.Create(collectionName, partitionName,
                new List<Field>()
                {
                    Field.Create(nameof(bookIds),bookIds),
                    Field.Create(nameof(wordCounts),wordCounts),
                    Field.CreateBinaryVectors(nameof(bookIntros),bookIntros),
                });

            return insertParam;
        }

        private void CreateBookCollection()
        {
            var hasBookCollection = MilvusClient.HasCollection(Book);
            if (!hasBookCollection.Data)
            {
                var r = MilvusClient.CreateCollection(
                    CreateCollectionParam.Create(
                        Book,
                        2,
                        new List<FieldType>()
                        {
                            FieldType.Create(
                                "bookIds",
                                DataType.Int64,
                                isPrimaryLey:true),
                            FieldType.Create("wordCounts",DataType.Int64),
                            FieldType.Create(
                                "bookIntros",
                                "",
                                DataType.FloatVector,
                                dimension:2,
                                0)
                        }));

                AssertRpcStatus(r);
            }
        }

        [TestMethod()]
        [DataRow(Book, HostConfig.DefaultTestPartitionName)]
        public void AInsertTest(string collectionName, string partitionName)
        {
            CreateBookCollection();

            var data = PreareData(collectionName, partitionName);

            var hasP = MilvusClient.HasPartition(HasPartitionParam.Create(collectionName, partitionName));
            if (!hasP.Data)
            {
                var createP = MilvusClient.CreatePartition(CreatePartitionParam.Create(collectionName, partitionName));
                AssertRpcStatus(createP);
            }

            var r = MilvusClient.Insert(data);

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Param.Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.SuccIndex.Count > 0);
        }

        [TestMethod()]
        [DataRow(HostConfig.DefaultTestCollectionName, HostConfig.DefaultTestPartitionName)]
        public void BDeleteTest(string collectionName, string partitionName)
        {
            var r = MilvusClient.Delete(DeleteParam.Create(collectionName, $"bookIds != 0"));

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Param.Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.SuccIndex.Count > 0);
        }

        [TestMethod()]
        [DataRow(Book)]
        public void GetCompactionStateTest(string collectionName)
        {
            var r = MilvusClient.ManualCompaction(collectionName);

            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Param.Status.Success, r.Exception?.ToString());

            var stateR = MilvusClient.GetCompactionState(r.Data.CompactionID);
            Assert.IsNotNull(stateR);
            Assert.IsTrue(stateR.Status == Param.Status.Success, stateR.Exception?.ToString());
        }
    }
}
