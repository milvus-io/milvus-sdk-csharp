using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Converter a default json format to a milvus dictionary format.
/// </summary>
public sealed class MilvusDictionaryConverter : JsonConverter<IDictionary<string, string>>
{
    /// <summary>
    /// Read a milvus dictionary format to a default json format.
    /// </summary>
    public override IDictionary<string, string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        Dictionary<string, string> dictionary = new();

        while (reader.Read())
        {
            // Get the key.
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                continue;
            }

            string key = reader.GetString();

            // Get the value.
            reader.Read();
            reader.Read();

            string value = reader.GetString();

            // Add to dictionary.
            dictionary.Add(key, value);
        }

        throw new JsonException();
    }

    /// <summary>
    /// Write a default json format to a milvus dictionary format.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IDictionary<string, string> value,
        JsonSerializerOptions options)
    {
        Verify.NotNull(writer);

        if (value == null)
        {
            return;
        }

        writer.WriteStartArray();

        foreach (KeyValuePair<string, string> keyValue in value)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("key");
            writer.WriteStringValue(keyValue.Key);

            writer.WritePropertyName("value");
            writer.WriteStringValue(keyValue.Value);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}