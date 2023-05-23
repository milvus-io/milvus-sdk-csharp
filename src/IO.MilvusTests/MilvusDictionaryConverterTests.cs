using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace IO.Milvus.Tests;


[TestClass()]
public class MilvusDictionaryConverterTests
{
    internal class TestDic
    {
        public string Name { get; set; } = "Test";

        [JsonConverter(typeof(MilvusDictionaryConverter))]
        public IDictionary<string, string>? KeyValuePairs { get; set; }
    }

    [TestMethod()]
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

        Assert.IsNotNull(dic);
        Assert.IsTrue(dic.KeyValuePairs.Any());
    }

    [TestMethod()]
    public void WriteTest()
    {
        var dic = new TestDic() { 
            KeyValuePairs = new Dictionary<string,string> { { "dim","128"} }
        };

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.IsNotNull(dic);
        Assert.IsTrue(dic.KeyValuePairs?.Any() == true);
    }

    [TestMethod()]
    public void WriteNullTest()
    {
        var dic = new TestDic();

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.IsNotNull(dic);
    }

    [TestMethod()]
    public void WriteNothinTest()
    {
        var dic = new TestDic() { KeyValuePairs = new Dictionary<string, string>() };

        var data = JsonSerializer.Serialize(dic);

        dic = JsonSerializer.Deserialize<TestDic>(data);

        Assert.IsNotNull(dic);
    }
}