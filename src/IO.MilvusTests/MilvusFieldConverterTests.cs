using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace IO.Milvus.Tests;

public class MilvusFieldConverterTests
{
    public class TestJsonFieldData
    {
        [JsonPropertyName("fields_data")]
        [JsonConverter(typeof(MilvusFieldConverter))]
        public IList<Field>? FieldData { get; set; }
    }

    [Fact()]
    public void ReadBoolTest()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 1,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "BoolData": {
                                        "data": [
                                            true,
                                            false
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<bool>>();
        (data.FieldData.First() as Field<bool>).Data.First().Should().BeTrue();
        (data.FieldData.First() as Field<bool>).Data.Last().Should().BeFalse();
    }

    [Fact()]
    public void ReadInt8Test()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 2,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "IntData": {
                                        "data": [
                                            2,
                                            3
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<sbyte>>();
        (data.FieldData.First() as Field<sbyte>).Data.First().Should().Be(2);
    }

    [Fact()]
    public void ReadInt16Test()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 3,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "IntData": {
                                        "data": [
                                            2,
                                            3
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<Int16>>();
        (data.FieldData.First() as Field<Int16>).Data.First().Should().Be(2);
    }

    [Fact()]
    public void ReadIntTest()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 4,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "IntData": {
                                        "data": [
                                            1141,
                                            1979
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<int>>();
    }

    [Fact()]
    public void ReadLongTest()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 5,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "LongData": {
                                        "data": [
                                            1141,
                                            1979
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<long>>();
    }

    [Fact()]
    public void ReadFloatTest()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 10,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "FloatData": {
                                        "data": [
                                            1141.1,
                                            1979.2
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<float>>();
        (data.FieldData.First() as Field<float>).Data[0].Should().Be(1141.1f);
    }

    [Fact()]
    public void ReadDoubleTest()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 11,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "DoubleData": {
                                        "data": [
                                            1141.1,
                                            1979.2
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<double>>();
        (data.FieldData.First() as Field<double>).Data[0].Should().Be(1141.1d);
    }

    [Fact()]
    public void ReadVarcharTest()
    {
        string responseData =
            """
            {
                "fields_data": [
                    {
                        "type": 21,
                        "field_name": "book_id",
                        "Field": {
                            "Scalars": {
                                "Data": {
                                    "StringData": {
                                        "data": [
                                            "od",
                                            "id"
                                        ]
                                    }
                                }
                            }
                        },
                        "field_id": 100
                    }
                ]
            }
            """;

        var data = JsonSerializer.Deserialize<TestJsonFieldData>(responseData);

        data.Should().NotBeNull();
        data.FieldData.Count.Should().Be(1);
        data.FieldData.First().FieldName.Should().Be("book_id");
        data.FieldData.First().FieldId.Should().Be(100);
        data.FieldData.First().Should().BeOfType<Field<string>>();
        (data.FieldData.First() as Field<string>).Data[0].Should().Be("od");
    }

    [Fact]
    public void ReadFloatVectorTest()
    {

    }

    [Fact]
    public void ReadBinaryVectorTest()
    {

    }
}