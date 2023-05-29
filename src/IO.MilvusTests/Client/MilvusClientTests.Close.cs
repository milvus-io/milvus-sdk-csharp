using FluentAssertions;
using IO.Milvus;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Fact]
    public async Task CloseTest()
    {
        var clients = MilvusConfig
            .Load()
            .Select(p => p.CreateClient())
            .ToList();

        foreach (var client in clients)
        {
            MilvusHealthState state = await client.HealthAsync();
            state.IsHealthy.Should().BeTrue();

            client.Close();

            bool exceptionThrown = false;
            try
            {
                state = await client.HealthAsync();
            }
            catch (ObjectDisposedException)
            {
                exceptionThrown = true;
            }

            if (!exceptionThrown)
            {
                Assert.Fail("Close failed");
            }
        }
    }
}
