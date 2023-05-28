using System.Text.Json.Serialization;
using System.Text.Json;
using Xunit;
using IO.Milvus.ApiSchema;

namespace IO.Milvus.Tests;

public class MilvusDictionaryConverterTests
{
    internal class TestDic
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

        var dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.NotNull(dic);
        Assert.True(dic.KeyValuePairs.Any());
    }

    [Fact]
    public void WriteTest()
    {
        var dic = new TestDic() { 
            KeyValuePairs = new Dictionary<string,string> { { "dim","128"} }
        };

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.NotNull(dic);
        Assert.True(dic.KeyValuePairs?.Any() == true);
    }

    [Fact]
    public void WriteNullTest()
    {
        var dic = new TestDic();

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.NotNull(dic);
    }

    [Fact]
    public void WriteNothinTest()
    {
        var dic = new TestDic() { KeyValuePairs = new Dictionary<string, string>() };

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.NotNull(dic);
    }
}