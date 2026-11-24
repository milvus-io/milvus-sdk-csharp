using Xunit;

namespace Milvus.Client.Tests;

public class StrongNameTests
{
    [Fact]
    public void Assembly_is_public_signed_with_the_expected_public_key_token()
    {
        byte[]? token = typeof(MilvusClient).Assembly.GetName().GetPublicKeyToken();
        Assert.NotNull(token);

        string hex = BitConverter.ToString(token!).Replace("-", "").ToLowerInvariant();
        Assert.Equal("935508312d658e9a", hex);
    }
}
