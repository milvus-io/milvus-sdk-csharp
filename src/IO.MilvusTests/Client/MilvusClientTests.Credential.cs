using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task CredentialTest(IMilvusClient2 milvusClient)
    {
        if(milvusClient.ToString().Contains("zilliz"))
        {
            return;
        }

        await milvusClient.CreateCredentialAsync("a", "b");

        IList<string> users = await milvusClient.ListCredUsersAsync();

        Assert.NotNull(users);
        Assert.True(users.Any());
    }
}
