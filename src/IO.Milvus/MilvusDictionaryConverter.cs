using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Milvus;

internal class MilvusDictionaryConverter : JsonConverter<IDictionary<string, string>>
{
    public override IDictionary<string, string> Read(
        ref Utf8JsonReader reader, 
        Type typeToConvert, 
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        var dictionary = new Dictionary<string, string>();

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

    public override void Write(
        Utf8JsonWriter writer,
        IDictionary<string, string> value,
        JsonSerializerOptions options)
    {
        if (value == null)
        {
            return;
        }

        writer.WriteStartArray();

        foreach (var keyValue in value)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("key");
            writer.WriteString(JsonEncodedText.Encode(keyValue.Key),keyValue.Key);
        }

        writer.WriteEndArray();
    }
}
