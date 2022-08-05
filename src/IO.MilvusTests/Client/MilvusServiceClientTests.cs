using IO.Milvus.Client;
using IO.Milvus.Param;
using IO.MilvusTests;
using IO.MilvusTests.Client.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IO.Milvus.Client.Tests
{

    [TestClass()]
    public class MilvusServiceClientTests : MilvusServiceClientTestsBase
    {

        [TestMethod()]
        public void MilvusServiceClientTest()
        {
            var service = DefaultClient();

            Assert.IsNotNull(service);
            Assert.IsTrue(service.ClientIsReady());
        }

        [TestMethod()]
        public void CloseTest()
        {
            var service = NewClient();
            service.Close();
        }

        [TestMethod()]
        public async Task CloseAsyncTest()
        {
            var service = NewClient();
            await service.CloseAsync();
        }

        [TestMethod()]
        public void CreateGrpcDefaultClientTest()
        {
            var defualtClient = MilvusServiceClient.CreateGrpcDefaultClient(ConnectParam.Create(
                HostConfig.Host,
                HostConfig.Port
                ));

            Assert.IsNotNull(defualtClient);
        }
    }

}