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

        IList<string> users = await milvusClient.ListCredUsersAsync();

        await milvusClient.CreateCredentialAsync("abb1bW", "bbbB1.,");

        users = await milvusClient.ListCredUsersAsync();

        Assert.NotNull(users);
        Assert.True(users.Any());

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
