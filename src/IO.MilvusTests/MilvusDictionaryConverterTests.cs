using System.Text.Json.Serialization;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace IO.Milvus.Tests;

public class MilvusDictionaryConverterTests
{
    internal class TestJsonEntity
    {
        public string Name { get; set; } = "Test";

        [JsonConverter(typeof(MilvusDictionaryConverter))]
        public IDictionary<string, string>? KeyValuePairs { get; set; }
    }

    [Fact]
    public void ReadTest()
    {
        var data =
            """
                {
                    "KeyValuePairs":                    
                    [
                        {
                            "key": "dim",
                            "value": "128"
                        }
                    ]
                    
                }
                """;

        var dic = JsonSerializer.Deserialize<TestJsonEntity>(data);

        Assert.NotNull(dic);
        dic.KeyValuePairs.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WriteTest()
    {
        var dic = new TestJsonEntity()
        {
            KeyValuePairs = new Dictionary<string, string> { { "dim", "128" } }
        };

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestJsonEntity>(data);

        Assert.NotNull(dic);
        Assert.True(dic.KeyValuePairs?.Any() == true);
    }

    [Fact]
    public void WriteNullTest()
    {
        var dic = new TestJsonEntity();

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestJsonEntity>(data);

        Assert.NotNull(dic);
    }

    [Fact]
    public void WriteNothingTest()
    {
        var dic = new TestJsonEntity() { KeyValuePairs = new Dictionary<string, string>() };

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestJsonEntity>(data);

        Assert.NotNull(dic);
    }
}