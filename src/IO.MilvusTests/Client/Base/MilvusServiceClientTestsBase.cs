using Microsoft.VisualStudio.TestTools.UnitTesting;
using IO.Milvus.Param;
using IO.Milvus.Client;

namespace IO.MilvusTests.Client.Base
{
    public abstract class MilvusServiceClientTestsBase
    {
        private static MilvusServiceClient _milvusclient;

        protected MilvusServiceClient MilvusClient { get => _milvusclient ?? (_milvusclient = DefaultClient()); }

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
                    host: "192.168.100.139",
                    port: 19531));

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
            Assert.IsTrue(r.Status == Status.Success,r.Exception?.ToString());
            Assert.IsTrue(r.Data.Msg == RpcStatus.SUCCESS_MSG);
        }

    }

}