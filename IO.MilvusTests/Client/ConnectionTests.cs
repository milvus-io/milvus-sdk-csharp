using Grpc.Core;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class ConnectionTests
{
    // If this test is failing for you, that means you haven't enabled authorization in Milvus; follow the instructions
    // in https://milvus.io/docs/authenticate.md.
    [Fact]
    public async Task Auth_failure_with_wrong_password()
    {
        using var client = new MilvusClient(TestEnvironment.Address, TestEnvironment.Username, "incorrect_password");

        try
        {
            await client.GetCollection("foo").DropAsync();

            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                Assert.Fail("Authorization seems to be disabled in Milvus; follow the instructions in https://milvus.io/docs/authenticate.md");
            }
        }
        catch (RpcException) // TODO: We should maybe catch the RpcException and rethrow a MilvusException instead
        {
            // Expected behavior
        }
    }
}
