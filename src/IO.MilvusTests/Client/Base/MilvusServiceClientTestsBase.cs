using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.Milvus.Param;
using IO.Milvus.Client;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Dml;
using IO.Milvus.Param.Index;
using Milvus.Proto.Schema;

namespace IO.MilvusTests.Client.Base
{
    public abstract class MilvusServiceClientTestsBase
    {
        private static MilvusServiceClient _milvusclient;
        protected Random rd = new Random(DateTime.Now.Second);

        protected MilvusServiceClient MilvusClient
        {
            get => _milvusclient ?? (_milvusclient = DefaultClient());
        }

        public MilvusServiceClient DefaultClient()
        {
            _milvusclient ??= new MilvusServiceClient(
                HostConfig.ConnectParam);

            Assert.IsNotNull(MilvusClient);
            Assert.IsTrue(MilvusClient.ClientIsReady());

            return MilvusClient;
        }

        public MilvusServiceClient NewClient()
        {
            var client = new MilvusServiceClient(
                HostConfig.ConnectParam);

            Assert.IsNotNull(client);
            Assert.IsTrue(client.ClientIsReady());

            return client;
        }

        public MilvusServiceClient NewErrorClient()
        {
            var client = new MilvusServiceClient(
                ConnectParam.Create(
                    host: HostConfig.Host,
                    port: HostConfig.Port));

            Assert.IsNotNull(client);
            Assert.IsTrue(client.ClientIsReady());

            return client;
        }

        protected void AssertRBool(R<bool> r)
        {
            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data);
        }

        protected void AssertRpcStatus(R<RpcStatus> r)
        {
            Assert.IsNotNull(r);
            Assert.IsTrue(r.Status == Status.Success, r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);
        }


        protected R<RpcStatus> ThenDropCollection(string collectionName)
        {
            return MilvusClient.DropCollection(collectionName);
        }

        protected R<RpcStatus> GivenNoCollection(string collectionName)
        {
            return MilvusClient.DropCollection(collectionName);
        }

        protected R<RpcStatus> GivenCollection(string collectionName)
        {
            return CreateBookCollection(collectionName);
        }

        protected InsertParam PrepareData(string collectionName, string partitionName)
        {
            var r = new Random(DateTime.Now.Second);

            var bookIds = new List<long>();
            var wordCounts = new List<long>();
            var bookIntros = new List<List<float>>();
            var content = new List<string>();

            for (int i = 0; i < 2000; i++)
            {
                bookIds.Add(i);
                wordCounts.Add(i + 10000);
                content.Add($"content {i}");
                var vector = new List<float>();
                for (int k = 0; k < 2; ++k)
                {
                    vector.Add(r.Next());
                }

                bookIntros.Add(vector);
            }

            var insertParam = InsertParam.Create(collectionName, partitionName,
                new List<Field>()
                {
                    Field.Create(nameof(bookIds), bookIds),
                    Field.Create(nameof(wordCounts), wordCounts),
                    Field.Create(nameof(content), content),
                    Field.CreateBinaryVectors(nameof(bookIntros), bookIntros),
                });

            return insertParam;
        }

        protected R<RpcStatus> CreateBookCollection(string collectionName)
        {
            // var hasBookCollection = MilvusClient.HasCollection(collectionName);
            // if (hasBookCollection.Data) return;

            var r = MilvusClient.CreateCollection(
                CreateCollectionParam.Create(
                    collectionName,
                    2,
                    new List<FieldType>()
                    {
                        FieldType.Create(
                            "bookIds",
                            DataType.Int64,
                            isPrimaryLey: true),
                        FieldType.Create(
                            "content",
                            DataType.VarChar,
                            new Dictionary<string, string>
                            {
                                { Constant.VARCHAR_MAX_LENGTH, "100" }
                            }
                        ),
                        FieldType.Create("wordCounts", DataType.Int64),
                        FieldType.Create(
                            "bookIntros",
                            "",
                            DataType.FloatVector,
                            dimension: 2,
                            0)
                    }));

            AssertRpcStatus(r);

            return r;
        }

        protected void GivenBookIndex(string collectionName)
        {
            var createIndexResult = MilvusClient.CreateIndex(new CreateIndexParam
            {
                CollectionName = collectionName,
                IndexType = IndexType.AUTOINDEX,
                FieldName = "bookIntros",
                IndexName = $"{collectionName}_indexBook",
                MetricType = MetricType.L2,
                ExtraDic = { { "params", "{\"nlist\":1024}" } }
            });
        }
    }
}
