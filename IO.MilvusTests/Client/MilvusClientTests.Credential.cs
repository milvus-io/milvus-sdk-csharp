using FluentAssertions;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Fact]
    public async Task CredentialTest()
    {
        if (Client.ToString().Contains("zilliz"))
        {
            return;
        }

        string username = "abb1bW";
        string password = "bbbB1.,";

        //Check
        IList<string> users = await Client.ListCredUsersAsync();
        users.Should().NotBeNullOrEmpty();
        if (users.Contains(username))
        {
            await Client.DeleteCredentialAsync(username);
        }

        //Create
        await Client.CreateCredentialAsync(username, password);
        users = await Client.ListCredUsersAsync();
        users.Should().NotBeNullOrEmpty();
        users.Should().Contain("abb1bW");

        //Delete
        await Client.DeleteCredentialAsync(username);
        users = await Client.ListCredUsersAsync();
        users.Should().NotBeNullOrEmpty();
        users.Should().NotContain("abb1bW");
    }
}
