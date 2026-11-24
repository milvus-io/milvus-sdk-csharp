using Xunit;
using Milvus.Client.V2;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class StrongNameTests
{
    [Fact]
    public void Assembly_is_public_signed_with_the_expected_public_key_token()
    {
        byte[]? token = typeof(MilvusClientV2).Assembly.GetName().GetPublicKeyToken();
        Assert.NotNull(token);

        string hex = BitConverter.ToString(token!).Replace("-", "").ToLowerInvariant();
        Assert.Equal("935508312d658e9a", hex);
    }
}
