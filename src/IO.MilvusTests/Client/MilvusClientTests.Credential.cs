using FluentAssertions;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task CredentialTest(IMilvusClient milvusClient)
    {
        if(milvusClient.ToString().Contains("zilliz"))
        {
            return;
        }

        string username = "abb1bW";
        string password = "bbbB1.,";

        //Check
        IList<string> users = await milvusClient.ListCredUsersAsync();
        users.Should().NotBeNullOrEmpty();
        if (users.Contains(username))
        {
            await milvusClient.DeleteCredential(username);
        }

        //Create
        await milvusClient.CreateCredentialAsync(username,password);
        users = await milvusClient.ListCredUsersAsync();
        users.Should().NotBeNullOrEmpty();
        users.Should().Contain("abb1bW");

        //Delete
        await milvusClient.DeleteCredential(username);
        users = await milvusClient.ListCredUsersAsync();
        users.Should().NotBeNullOrEmpty();
        users.Should().NotContain("abb1bW");
    }
}
