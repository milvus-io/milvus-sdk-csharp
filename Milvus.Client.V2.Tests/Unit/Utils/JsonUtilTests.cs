using System.Text.Json;

using Xunit;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Unit.Utils;

[Trait("Category", "Unit")]
public class JsonUtilTests
{
    private enum SampleEnum
    {
        Zero = 0,
        One = 1
    }

    private sealed class SamplePoco
    {
        public string? Name { get; set; }

        public int Count { get; set; }
    }

    [Fact]
    public void Serialize_matches_JsonSerializer_for_a_value_graph()
    {
        var graph = new Dictionary<string, object?>
        {
            ["text"] = "a\"b<é",
            ["flag"] = true,
            ["neg"] = -42,
            ["ulong"] = 4_000_000_000L,
            ["small"] = (short)7,
            ["tiny"] = (byte)3,
            ["sb"] = (sbyte)-1,
            ["us"] = (ushort)9,
            ["single"] = 1.5f,
            ["double"] = 2.25,
            ["money"] = 12.34m,
            ["char"] = 'x',
            ["empty"] = null,
            ["guid"] = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            ["uri"] = new Uri("http://example.com/%7Euser?q=a%2Fb"),
            ["when"] = new DateTimeOffset(2023, 1, 2, 3, 4, 5, TimeSpan.FromHours(8)),
            ["elapsed"] = TimeSpan.FromSeconds(90),
            ["base64"] = new byte[] { 0x00, 0x0F, 0xFF },
            ["enum"] = SampleEnum.One,
            ["nested"] = new Dictionary<string, object?>
            {
                ["list"] = new object?[] { "one", 2, false },
                ["ints"] = new[] { 1L, 2L, 3L },
                ["strings"] = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" },
                ["intdict"] = new Dictionary<string, int> { ["count"] = 5 },
                ["longdict"] = new Dictionary<string, long> { ["big"] = 4_000_000_000L },
                ["json"] = JsonDocument.Parse("{\"k\":[true,1]}").RootElement.Clone()
            }
        };

        string expected = JsonSerializer.Serialize(graph);
        Assert.Equal(expected, JsonUtil.Serialize(graph));
    }

    [Fact]
    public void SerializeToUtf8Bytes_matches_JsonSerializer()
    {
        var value = new Dictionary<string, object?>
        {
            ["a"] = new[] { 1L, 2L },
            ["b"] = new Dictionary<string, object?> { ["c"] = "nested\"value" }
        };

        byte[] expected = JsonSerializer.SerializeToUtf8Bytes(value);
        Assert.Equal(expected, JsonUtil.SerializeToUtf8Bytes(value));
    }

    [Fact]
    public void Serialize_writes_empty_dictionary_as_object_not_null()
    {
        Assert.Equal("{}", JsonUtil.Serialize(new Dictionary<string, object?>()));
        Assert.Equal("null", JsonUtil.Serialize(null));
    }

    [Fact]
    public void Serialize_rejects_arbitrary_objects_with_a_clear_error()
    {
        // No POCO fallback: serializing an arbitrary object needs reflection that trimming removes, so it is
        // rejected instead of being silently written as "{}" on the wire.
        Assert.Throws<NotSupportedException>(() => JsonUtil.Serialize(new SamplePoco { Name = "x", Count = 1 }));
    }

    [Fact]
    public void Serialize_serializes_string_keyed_and_non_generic_dictionaries_as_objects()
    {
        Assert.Equal("{\"a\":\"1\",\"b\":\"2\"}", JsonUtil.Serialize(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }));

        var nonGeneric = new System.Collections.Hashtable { ["k"] = 1.5 };
        Assert.Equal("{\"k\":1.5}", JsonUtil.Serialize(nonGeneric));
    }

    [Fact]
    public void Serialize_serializes_enums_as_numbers()
    {
        Assert.Equal("0", JsonUtil.Serialize(SampleEnum.Zero));
        Assert.Equal("1", JsonUtil.Serialize(SampleEnum.One));
    }

    [Fact]
    public void Serialize_serializes_byte_array_as_base64()
    {
        Assert.Equal("\"AA//\"", JsonUtil.Serialize(new byte[] { 0x00, 0x0F, 0xFF }));
    }

    [Fact]
    public void Serialize_passes_json_element_through_unchanged()
    {
        using JsonDocument document = JsonDocument.Parse("{\"a\":[1,2],\"b\":true}");
        Assert.Equal("{\"a\":[1,2],\"b\":true}", JsonUtil.Serialize(document.RootElement));
    }

    [Fact]
    public void Serialize_throws_for_cyclic_values_instead_of_stack_overflowing()
    {
        var self = new Dictionary<string, object?>();
        self["self"] = self;

        Assert.Throws<NotSupportedException>(() => JsonUtil.Serialize(self));
    }
}
