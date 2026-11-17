using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Serializes values to JSON without reflection-based serialization. System.Text.Json's reflection-based type
/// resolver is disabled in trimmed and NativeAOT apps (<c>JsonSerializerIsReflectionEnabledByDefault</c>), so
/// the SDK's own JSON writes go through this writer instead. Values are dispatched by their CLR shape (scalars,
/// strings, string-keyed dictionaries, enumerables, <see cref="JsonElement" /> and byte arrays) with no
/// reflection. The output is byte-identical to the default, non-indented <see cref="JsonSerializer" /> output
/// for these shapes; any other object is rejected with <see cref="NotSupportedException" /> (pass a dictionary,
/// a <see cref="JsonElement" />, or a pre-encoded JSON string instead).
/// </summary>
internal static class JsonUtil
{
    private const int MaxDepth = 256;

    /// <summary>
    /// Serializes a value to a compact JSON string, matching the default (non-indented)
    /// <c>JsonSerializer.Serialize(value)</c> output.
    /// </summary>
    internal static string Serialize(object? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteValue(writer, value, 0);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Serializes a value to UTF-8 JSON bytes, matching <c>JsonSerializer.SerializeToUtf8Bytes(value)</c>.
    /// </summary>
    internal static byte[] SerializeToUtf8Bytes(object? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteValue(writer, value, 0);
        }

        return stream.ToArray();
    }

    internal static void WriteValue(Utf8JsonWriter writer, object? value)
        => WriteValue(writer, value, 0);

    private static void WriteValue(Utf8JsonWriter writer, object? value, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new NotSupportedException(
                $"JSON value nesting exceeds the maximum depth of {MaxDepth}; a cyclic or deeply nested value cannot be serialized.");
        }

        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case long number:
                writer.WriteNumberValue(number);
                break;
            case short number:
                writer.WriteNumberValue(number);
                break;
            case byte number:
                writer.WriteNumberValue(number);
                break;
            case sbyte number:
                writer.WriteNumberValue(number);
                break;
            case uint number:
                writer.WriteNumberValue(number);
                break;
            case ushort number:
                writer.WriteNumberValue(number);
                break;
            case ulong number:
                writer.WriteNumberValue(number);
                break;
            case float number:
                writer.WriteNumberValue(number);
                break;
            case double number:
                writer.WriteNumberValue(number);
                break;
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            case char character:
                writer.WriteStringValue(character.ToString());
                break;
            case DateTime dateTime:
                writer.WriteStringValue(dateTime);
                break;
            case DateTimeOffset dateTimeOffset:
                writer.WriteStringValue(dateTimeOffset);
                break;
            case TimeSpan timeSpan:
                writer.WriteStringValue(timeSpan.ToString());
                break;
            case Guid guid:
                writer.WriteStringValue(guid);
                break;
            case Uri uri:
                // Match System.Text.Json's UriConverter, which writes OriginalString (ToString() unescapes
                // %xx sequences, e.g. "%7E" -> "~", so it is not byte-identical for escaped Uris).
                writer.WriteStringValue(uri.OriginalString);
                break;
            case byte[] bytes:
                writer.WriteStringValue(Convert.ToBase64String(bytes));
                break;
            case Enum enumValue:
                WriteEnumValue(writer, enumValue);
                break;
            case IDictionary<string, object?> dictionary:
                WriteObject(writer, dictionary, depth);
                break;
            case IReadOnlyDictionary<string, object?> dictionary:
                WriteObject(writer, dictionary, depth);
                break;
            case IDictionary<string, string> dictionary:
                WriteStringObject(writer, dictionary, depth);
                break;
            case IReadOnlyDictionary<string, string> dictionary:
                WriteStringObject(writer, dictionary, depth);
                break;
            case IDictionary dictionary:
                WriteNonGenericObject(writer, dictionary, depth);
                break;
            case IEnumerable enumerable:
                WriteArray(writer, enumerable, depth);
                break;
            default:
                // No reflection fallback: serializing an arbitrary object would need the reflection metadata
                // that trimming removes (and System.Text.Json's reflection resolver, which AOT disables). Reject
                // it up front with a clear error instead of silently emitting an empty object on the wire.
                throw new NotSupportedException(
                    $"JSON cannot serialize a value of type {value.GetType().FullName}; pass a primitive, a string-keyed " +
                    "dictionary, a collection, a JsonElement, or a pre-encoded JSON string instead.");
        }
    }

    private static void WriteObject(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object?>> pairs, int depth)
    {
        writer.WriteStartObject();
        foreach (KeyValuePair<string, object?> pair in pairs)
        {
            writer.WritePropertyName(pair.Key);
            WriteValue(writer, pair.Value, depth + 1);
        }

        writer.WriteEndObject();
    }

    private static void WriteStringObject(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, string>> pairs, int depth)
    {
        writer.WriteStartObject();
        foreach (KeyValuePair<string, string> pair in pairs)
        {
            writer.WritePropertyName(pair.Key);
            WriteValue(writer, pair.Value, depth + 1);
        }

        writer.WriteEndObject();
    }

    private static void WriteNonGenericObject(Utf8JsonWriter writer, IDictionary dictionary, int depth)
    {
        writer.WriteStartObject();
        foreach (DictionaryEntry entry in dictionary)
        {
            writer.WritePropertyName(Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? "");
            WriteValue(writer, entry.Value, depth + 1);
        }

        writer.WriteEndObject();
    }

    private static void WriteArray(Utf8JsonWriter writer, IEnumerable enumerable, int depth)
    {
        writer.WriteStartArray();
        foreach (object? item in enumerable)
        {
            WriteValue(writer, item, depth + 1);
        }

        writer.WriteEndArray();
    }

    private static void WriteEnumValue(Utf8JsonWriter writer, object value)
    {
        if (Enum.GetUnderlyingType(value.GetType()) == typeof(ulong))
        {
            writer.WriteNumberValue(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }
}
